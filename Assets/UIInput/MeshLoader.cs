using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class MeshLoader
{
    // Very small OBJ loader: supports v, vn, vt (optional), f triangles/quads.
    public static Mesh LoadObj(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();

        var finalVerts = new List<Vector3>();
        var finalNorms = new List<Vector3>();
        var finalUvs = new List<Vector2>();
        var tris = new List<int>();

        var vertMap = new Dictionary<string, int>();

        var lines = File.ReadAllLines(path);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0])
            {
                case "v":
                    verts.Add(ParseV3(parts));
                    break;
                case "vn":
                    norms.Add(ParseV3(parts));
                    break;
                case "vt":
                    uvs.Add(ParseV2(parts));
                    break;
                case "f":
                    // f can be tri or quad; we fan triangulate.
                    var face = new List<int>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var key = parts[i];
                        if (!vertMap.TryGetValue(key, out int idx))
                        {
                            ParseFaceKey(key, verts, uvs, norms,
                                out var v, out var uv, out var n);

                            idx = finalVerts.Count;
                            finalVerts.Add(v);
                            finalUvs.Add(uv);
                            finalNorms.Add(n);
                            vertMap[key] = idx;
                        }
                        face.Add(idx);
                    }
                    for (int i = 1; i < face.Count - 1; i++)
                    {
                        tris.Add(face[0]);
                        tris.Add(face[i]);
                        tris.Add(face[i + 1]);
                    }
                    break;
            }
        }

        var mesh = new Mesh();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        mesh.SetVertices(finalVerts);
        if (finalUvs.Count == finalVerts.Count) mesh.SetUVs(0, finalUvs);
        if (finalNorms.Count == finalVerts.Count) mesh.SetNormals(finalNorms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        if (finalNorms.Count != finalVerts.Count) mesh.RecalculateNormals();

        return mesh;
    }

    static Vector3 ParseV3(string[] p)
    {
        return new Vector3(
            float.Parse(p[1], CultureInfo.InvariantCulture),
            float.Parse(p[2], CultureInfo.InvariantCulture),
            float.Parse(p[3], CultureInfo.InvariantCulture)
        );
    }

    static Vector2 ParseV2(string[] p)
    {
        return new Vector2(
            float.Parse(p[1], CultureInfo.InvariantCulture),
            float.Parse(p[2], CultureInfo.InvariantCulture)
        );
    }

    static void ParseFaceKey(
        string key,
        List<Vector3> verts,
        List<Vector2> uvs,
        List<Vector3> norms,
        out Vector3 v,
        out Vector2 uv,
        out Vector3 n)
    {
        // formats: v, v/vt, v//vn, v/vt/vn
        var seg = key.Split('/');
        int vi = int.Parse(seg[0]) - 1;
        v = verts[vi];

        uv = Vector2.zero;
        if (seg.Length > 1 && seg[1] != "" && uvs.Count > 0)
        {
            int uvi = int.Parse(seg[1]) - 1;
            if (uvi >= 0 && uvi < uvs.Count) uv = uvs[uvi];
        }

        n = Vector3.up;
        if (seg.Length > 2 && seg[2] != "" && norms.Count > 0)
        {
            int ni = int.Parse(seg[2]) - 1;
            if (ni >= 0 && ni < norms.Count) n = norms[ni];
        }
    }

    public static Mesh LoadStl(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        using var fs = File.OpenRead(path);

        // Quick check: binary STL has 80-byte header + 4-byte tri count.
        // ASCII STL starts with "solid".
        bool isAscii = IsAsciiStl(fs);

        fs.Position = 0;
        return isAscii ? LoadAsciiStl(fs, Path.GetFileNameWithoutExtension(path))
                       : LoadBinaryStl(fs, Path.GetFileNameWithoutExtension(path));
    }

    static bool IsAsciiStl(FileStream fs)
    {
        if (fs.Length < 6) return false;
        byte[] start = new byte[5];
        fs.Read(start, 0, 5);
        string s = System.Text.Encoding.ASCII.GetString(start).ToLowerInvariant();
        // Many binary STLs also start with "solid", but they usually contain non-ASCII bytes later.
        // We'll do a safer heuristic:
        if (s != "solid") return false;

        fs.Position = 0;
        using var reader = new StreamReader(fs, System.Text.Encoding.ASCII, true, 1024, true);
        string firstLine = reader.ReadLine();
        if (firstLine == null) return false;

        // If file contains "facet normal" soon after, likely ASCII.
        for (int i = 0; i < 20; i++)
        {
            var line = reader.ReadLine();
            if (line == null) break;
            if (line.TrimStart().StartsWith("facet normal", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static Mesh LoadBinaryStl(Stream stream, string meshName)
    {
        using var br = new BinaryReader(stream);

        // 80-byte header
        br.ReadBytes(80);
        uint triCount = br.ReadUInt32();

        var verts = new Vector3[triCount * 3];
        var norms = new Vector3[triCount * 3];
        var tris  = new int[triCount * 3];

        for (uint i = 0; i < triCount; i++)
        {
            Vector3 n = ReadVec3(br);

            Vector3 v0 = ReadVec3(br);
            Vector3 v1 = ReadVec3(br);
            Vector3 v2 = ReadVec3(br);

            int baseIndex = (int)(i * 3);
            verts[baseIndex + 0] = v0;
            verts[baseIndex + 1] = v1;
            verts[baseIndex + 2] = v2;

            norms[baseIndex + 0] = n;
            norms[baseIndex + 1] = n;
            norms[baseIndex + 2] = n;

            tris[baseIndex + 0] = baseIndex + 0;
            tris[baseIndex + 1] = baseIndex + 1;
            tris[baseIndex + 2] = baseIndex + 2;

            // attribute byte count (unused)
            br.ReadUInt16();
        }

        var mesh = new Mesh();
        mesh.name = meshName;
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        return mesh;
    }

    static Mesh LoadAsciiStl(Stream stream, string meshName)
    {
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        using var sr = new StreamReader(stream);
        string line;
        int triIndex = 0;

        while ((line = sr.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    verts.Add(new Vector3(x, y, z));
                }
            }
            else if (line.StartsWith("endfacet", StringComparison.OrdinalIgnoreCase))
            {
                // Every facet should have exactly 3 vertices
                tris.Add(triIndex + 0);
                tris.Add(triIndex + 1);
                tris.Add(triIndex + 2);
                triIndex += 3;
            }
        }

        var mesh = new Mesh();
        mesh.name = meshName;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    static Vector3 ReadVec3(BinaryReader br)
    {
        float x = br.ReadSingle();
        float y = br.ReadSingle();
        float z = br.ReadSingle();
        return new Vector3(x, y, z);
    }
}

