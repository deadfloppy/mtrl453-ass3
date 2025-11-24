# URP Post-Processing Setup Guide

## What Was Fixed

The motion blur and bloom effects weren't working because they used `OnRenderImage()`, which only works with Unity's Built-in Render Pipeline. Your project uses URP (Universal Render Pipeline), which requires **Scriptable Renderer Features**.

**UPDATE**: Both render features now support Unity's **RenderGraph API** (Unity 2022.2+), so they work with modern URP versions without needing Compatibility Mode.

## New Files Created

1. **MotionBlurRenderFeature.cs** - URP-compatible motion blur
2. **BloomRenderFeature.cs** - URP-compatible bloom

## Setup Instructions

### Step 1: Add Render Features to URP Renderer

1. In Unity, go to **Project** window
2. Navigate to `Assets/Settings/`
3. Select `PC_Renderer.asset` (or whichever renderer you're using)
4. In the Inspector, scroll down to **Renderer Features**
5. Click **Add Renderer Feature** → Select **Motion Blur Render Feature**
6. Click **Add Renderer Feature** → Select **Bloom Render Feature**

### Step 2: Configure Motion Blur Settings

In the `PC_Renderer` Inspector, you'll now see:

**Motion Blur Render Feature:**
- **Blur Amount**: 0.7 (default) - controls motion trail length
- **Use Enhanced Mode**: Toggle for extreme blur
- **Enhanced Blur Amount**: 0.95 - very strong blur for visualization
- **Render Pass Event**: Before Rendering Post Processing (default)

### Step 3: Configure Bloom Settings

**Bloom Render Feature:**
- **Intensity**: 2.0 - brightness of glow
- **Threshold**: 0.5 - brightness threshold for bloom
- **Iterations**: 2 - blur quality
- **Blur**: 2.0 - bloom spread
- **Show Bloom Only**: Toggle for debugging

### Step 4: Remove Old Components (Optional)

The old `SimpleMotionBlur.cs` and `SimpleBloom.cs` scripts on your Camera won't do anything anymore (but they won't cause errors either). You can:

1. Select your Main Camera in the scene
2. Remove the `Simple Motion Blur` component
3. Remove the `Simple Bloom` component

### Step 5: Test

1. Enter Play Mode
2. You should now see motion blur working!
3. Adjust the settings in the Renderer asset to tune the effect

## Linking to Helicoid's Enhanced Mode

If you want the motion blur to automatically sync with the `HelicoidVolumetricDisplay.enhancedVisualizationMode`, you'll need to:

1. Add a script that finds the `MotionBlurRenderFeature` at runtime
2. Update its `useEnhancedMode` setting based on the helicoid's setting

Let me know if you need help with this!

## Troubleshooting

**Q: I don't see the Render Features in the dropdown**
- Make sure the scripts compiled without errors (check Console)
- Try reimporting the scripts

**Q: Effects still don't work**
- Make sure you added them to the correct Renderer asset
- Check that your camera is using the URP renderer
- Verify no errors in Console

**Q: Performance is slow**
- Reduce Bloom iterations
- Lower Blur Amount
- Reduce the blur buffer resolution
