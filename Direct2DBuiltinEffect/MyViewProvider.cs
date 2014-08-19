///////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012-2014 Rodrigo 'r2d2rigo' Díaz
// Images used in this sample (C) www.kenney.nl
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
///////////////////////////////////////////////////////////////////////////////

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.WIC;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;

namespace Direct2DBuiltinEffect
{
    /// <summary>
    /// The view provider class that will handle all the view operations (update/draw).
    /// </summary>
    internal class MyViewProvider : IFrameworkView
    {
        private CoreWindow window;
        private SharpDX.Direct3D11.Device1 device;
        private SharpDX.Direct3D11.DeviceContext1 d3dContext;
        private SharpDX.Direct2D1.DeviceContext d2dContext;
        private SwapChain1 swapChain;
        private SharpDX.Direct2D1.Bitmap1 d2dTarget;

        private SharpDX.Direct2D1.Bitmap1 playerBitmap;
        private SharpDX.Direct2D1.Bitmap1 terrainBitmap;
        private SharpDX.Direct2D1.ImageBrush terrainBrush;

        private SharpDX.Direct2D1.Effects.HueRotation hueRotationEffect;

        private SharpDX.Direct2D1.Effects.Shadow shadowEffect;
        private SharpDX.Direct2D1.Effects.AffineTransform2D affineTransformEffect;
        private SharpDX.Direct2D1.Effects.Composite compositeEffect;

        private SharpDX.Direct2D1.Bitmap1 brushTarget;

        /// <summary>
        /// This function is called before SetWindow, so we can't do much yet.
        /// </summary>
        /// <param name="applicationView"></param>
        public void Initialize(CoreApplicationView applicationView)
        {
        }

        /// <summary>
        /// Now that we have a CoreWindow object, the DirectX device/context can be created.
        /// </summary>
        /// <param name="entryPoint"></param>
        public void Load(string entryPoint)
        {
            // Get the default hardware device and enable debugging. Don't care about the available feature level.
            // DeviceCreationFlags.BgraSupport must be enabled to allow Direct2D interop.
            SharpDX.Direct3D11.Device defaultDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport);

            // Query the default device for the supported device and context interfaces.
            device = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            d3dContext = device.ImmediateContext.QueryInterface<SharpDX.Direct3D11.DeviceContext1>();

            // Query for the adapter and more advanced DXGI objects.
            SharpDX.DXGI.Device2 dxgiDevice2 = device.QueryInterface<SharpDX.DXGI.Device2>();
            SharpDX.DXGI.Adapter dxgiAdapter = dxgiDevice2.Adapter;
            SharpDX.DXGI.Factory2 dxgiFactory2 = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>();

            // Description for our swap chain settings.
            SwapChainDescription1 description = new SwapChainDescription1()
            {
                // 0 means to use automatic buffer sizing.
                Width = 0,
                Height = 0,
                // 32 bit RGBA color.
                Format = Format.B8G8R8A8_UNorm,
                // No stereo (3D) display.
                Stereo = false,
                // No multisampling.
                SampleDescription = new SampleDescription(1, 0),
                // Use the swap chain as a render target.
                Usage = Usage.RenderTargetOutput,
                // Enable double buffering to prevent flickering.
                BufferCount = 2,
                // No scaling.
                Scaling = Scaling.None,
                // Flip between both buffers.
                SwapEffect = SwapEffect.FlipSequential,
            };

            // Generate a swap chain for our window based on the specified description.
            swapChain = new SwapChain1(dxgiFactory2, device, new ComObject(window), ref description);

            // Get the default Direct2D device and create a context.
            SharpDX.Direct2D1.Device d2dDevice = new SharpDX.Direct2D1.Device(dxgiDevice2);
            d2dContext = new SharpDX.Direct2D1.DeviceContext(d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None);

            // Specify the properties for the bitmap that we will use as the target of our Direct2D operations.
            // We want a 32-bit BGRA surface with premultiplied alpha.
            BitmapProperties1 properties = new BitmapProperties1(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                DisplayProperties.LogicalDpi, DisplayProperties.LogicalDpi, BitmapOptions.Target | BitmapOptions.CannotDraw);

            // Get the default surface as a backbuffer and create the Bitmap1 that will hold the Direct2D drawing target.
            Surface backBuffer = swapChain.GetBackBuffer<Surface>(0);
            d2dTarget = new Bitmap1(d2dContext, backBuffer, properties);

            // Load bitmap images
            playerBitmap = this.LoadBitmapFromContentFile("/Assets/Bitmaps/player.png");
            terrainBitmap = this.LoadBitmapFromContentFile("/Assets/Bitmaps/terrain.png");

            // Create hue rotation effect
            hueRotationEffect = new SharpDX.Direct2D1.Effects.HueRotation(d2dContext);

            // Create image shadow effect
            shadowEffect = new SharpDX.Direct2D1.Effects.Shadow(d2dContext);

            // Create image transform effect
            affineTransformEffect = new SharpDX.Direct2D1.Effects.AffineTransform2D(d2dContext);
            affineTransformEffect.SetInputEffect(0, shadowEffect);
            affineTransformEffect.TransformMatrix = Matrix3x2.Translation(terrainBitmap.PixelSize.Width * 0.25f, terrainBitmap.PixelSize.Height * 0.25f);

            // Create composite effect
            compositeEffect = new SharpDX.Direct2D1.Effects.Composite(d2dContext);
            compositeEffect.InputCount = 2;
            compositeEffect.SetInputEffect(0, affineTransformEffect);

            // Create tiling brush for terrain bitmap
            terrainBrush = new ImageBrush(d2dContext, terrainBitmap, new ImageBrushProperties()
            {
                ExtendModeX = ExtendMode.Wrap,
                ExtendModeY = ExtendMode.Wrap,
                SourceRectangle = new RectangleF(0, 0, terrainBitmap.Size.Width, terrainBitmap.Size.Height),
            });

            // Create rendertarget for drawing the tiling brush
            brushTarget = new Bitmap1(d2dContext, new Size2((int)(terrainBitmap.Size.Width * 10), (int)terrainBitmap.Size.Height), new BitmapProperties1()
            {
                PixelFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                BitmapOptions = BitmapOptions.Target
            });
        }

        /// <summary>
        /// Run our application until the user quits.
        /// </summary>
        public void Run()
        {
            // Make window active and hide mouse cursor.
            window.PointerCursor = null;
            window.Activate();

            // Infinite loop to prevent the application from exiting.
            while (true)
            {
                // Dispatch all pending events in the queue.
                window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

                // Quit if the users presses Escape key.
                if (window.GetAsyncKeyState(VirtualKey.Escape) == CoreVirtualKeyStates.Down)
                {
                    return;
                }

                // Set the render target for drawing the brush
                d2dContext.Target = brushTarget;
                
                // Clear the rendertarget and draw the brush tiling 10 times
                d2dContext.BeginDraw();
                d2dContext.Clear(Color.Transparent);
                d2dContext.Transform = Matrix3x2.Identity;
                d2dContext.FillRectangle(new RectangleF(0, 0, terrainBitmap.Size.Width * 10, terrainBitmap.Size.Height), terrainBrush);
                d2dContext.EndDraw();

                // Set the Direct2D drawing target.
                d2dContext.Target = d2dTarget;

                // Clear the target and draw some geometry with the brushes we created. 
                d2dContext.BeginDraw();
                d2dContext.Clear(Color.CornflowerBlue);

                // Calculate the center of the screen.
                int halfWidth = this.swapChain.Description.ModeDescription.Width / 2;
                int halfHeight = this.swapChain.Description.ModeDescription.Height / 2;

                // Translate the origin of coordinates for drawing the bitmap filled rectangle.
                d2dContext.Transform = Matrix3x2.Translation(halfWidth - 350, halfHeight);

                // Update input images for shadow and composite effects, and draw the resulting image.
                shadowEffect.SetInput(0, brushTarget, true);
                compositeEffect.SetInput(1, brushTarget, true);
                d2dContext.DrawImage(compositeEffect);

                // Translate again for drawing the player bitmap.
                d2dContext.Transform = Matrix3x2.Translation(halfWidth, halfHeight - playerBitmap.Size.Height);

                // Update input image and value for hue rotation effect and draw it.
                hueRotationEffect.SetInput(0, playerBitmap, true);
                hueRotationEffect.Angle = System.DateTime.Now.Millisecond % 360;
                d2dContext.DrawImage(hueRotationEffect);

                d2dContext.EndDraw();

                // Present the current buffer to the screen.
                swapChain.Present(1, PresentFlags.None);
            }
        }

        /// <summary>
        /// Sets the window where the app will be rendered.
        /// </summary>
        /// <param name="window">Our main window</param>
        public void SetWindow(CoreWindow window)
        {
            this.window = window;
        }

        /// <summary>
        /// Dispose all the created objects.
        /// </summary>
        public void Uninitialize()
        {
            Utilities.Dispose(ref terrainBrush);
            Utilities.Dispose(ref terrainBitmap);
            Utilities.Dispose(ref playerBitmap);
            Utilities.Dispose(ref swapChain);
            Utilities.Dispose(ref d2dTarget);
            Utilities.Dispose(ref d3dContext);
            Utilities.Dispose(ref d2dContext);
            Utilities.Dispose(ref device);
        }

        /// <summary>
        /// Loads an existing image file into a SharpDX.Direct2D1.Bitmap1.
        /// </summary>
        /// <param name="filePath">Relative path to the content file.</param>
        /// <returns>Loaded bitmap.</returns>
        private SharpDX.Direct2D1.Bitmap1 LoadBitmapFromContentFile(string filePath)
        {
            SharpDX.Direct2D1.Bitmap1 newBitmap;

            // Neccessary for creating WIC objects.
            ImagingFactory imagingFactory = new ImagingFactory();
            NativeFileStream fileStream = new NativeFileStream(Package.Current.InstalledLocation.Path + filePath,
                NativeFileMode.Open, NativeFileAccess.Read);
            
            // Used to read the image source file.
            BitmapDecoder bitmapDecoder = new BitmapDecoder(imagingFactory, fileStream, DecodeOptions.CacheOnDemand);

            // Get the first frame of the image.
            BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            // Convert it to a compatible pixel format.
            FormatConverter converter = new FormatConverter(imagingFactory);
            converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA);

            // Create the new Bitmap1 directly from the FormatConverter.
            newBitmap = SharpDX.Direct2D1.Bitmap1.FromWicBitmap(d2dContext, converter);

            Utilities.Dispose(ref bitmapDecoder);
            Utilities.Dispose(ref fileStream);
            Utilities.Dispose(ref imagingFactory);

            return newBitmap;
        }
    }
}
