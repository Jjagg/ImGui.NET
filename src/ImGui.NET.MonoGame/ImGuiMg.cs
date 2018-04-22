using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Vector2 = System.Numerics.Vector2;

namespace ImGuiNET
{
    public sealed class ImGuiMg : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly List<Texture2D> _textures;
        private readonly RasterizerState _rasterizerState;
        private readonly SpriteEffect _spriteEffect;
        private KeyboardState _prevKeyState;

        private int _sFontTexture;
        private float _wheelPosition;
        private readonly float _scaleFactor;

        public ImGuiMg(GameWindow gw, GraphicsDevice gd)
        {
            _graphicsDevice = gd;
            _textures = new List<Texture2D>();
            _rasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };
            _spriteEffect = new SpriteEffect(gd);

            int desiredWidth = 960, desiredHeight = 540;

            _scaleFactor = (float) gd.PresentationParameters.BackBufferWidth / desiredWidth;

            ImGui.GetIO().FontAtlas.AddDefaultFont();

            SetKeyMappings();
            gw.TextInput += OnKeyPress;

            CreateDeviceObjects();
        }

        #region Keys

        private void OnKeyPress(object sender, TextInputEventArgs e)
        {
            Console.Write("Char typed: " + e.Character);
            ImGui.AddInputCharacter(e.Character);
        }

        private static unsafe void SetKeyMappings()
        {
            var io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Keys.Home;
            io.KeyMap[GuiKey.End] = (int)Keys.End;
            io.KeyMap[GuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Keys.Back;
            io.KeyMap[GuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[GuiKey.A] = (int)Keys.A;
            io.KeyMap[GuiKey.C] = (int)Keys.C;
            io.KeyMap[GuiKey.V] = (int)Keys.V;
            io.KeyMap[GuiKey.X] = (int)Keys.X;
            io.KeyMap[GuiKey.Y] = (int)Keys.Y;
            io.KeyMap[GuiKey.Z] = (int)Keys.Z;
        }

        private static void UpdateKeys(KeyboardState prev, KeyboardState current)
        {
            // Keys down
            foreach (var k in current.GetPressedKeys())
                ImGui.GetIO().KeysDown[(int) k] = true;

            // Keys up
            foreach (var k in prev.GetPressedKeys())
            {
                if (!current.GetPressedKeys().Contains(k))
                    ImGui.GetIO().KeysDown[(int) k] = false;
            }

            UpdateModifiers(current);
        }

        private static void UpdateModifiers(KeyboardState state)
        {
            var io = ImGui.GetIO();
            io.AltPressed = state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt);
            io.CtrlPressed = state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl);
            io.ShiftPressed = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
        }

        #endregion

        private unsafe void CreateDeviceObjects()
        {
            var io = ImGui.GetIO();

            // Build texture atlas
            var texData = io.FontAtlas.GetTexDataAsRGBA32();
            var tex = new Texture2D(_graphicsDevice, texData.Width, texData.Height);
            if (texData.BytesPerPixel != 4)
                throw new Exception();
            var pixelCount = texData.Width * texData.Height;
            var pixelData = new Color[pixelCount];
            for (var i = 0; i < pixelCount; i++)
                pixelData[i] = new Color((int) texData.Pixels[4 * i], (int) texData.Pixels[4 * i + 1],
                    (int) texData.Pixels[4 * i + 2], (int) texData.Pixels[4 * i + 3]);
            tex.SetData(pixelData);
            _sFontTexture = _textures.Count;
            _textures.Add(tex);
            io.FontAtlas.SetTexID(_sFontTexture);
            // Store the texture identifier in the ImFontAtlas substructure.
            io.FontAtlas.SetTexID(_sFontTexture);

            // Cleanup (don't clear the input data if you want to append new fonts later)
            //io.Fonts->ClearInputData();
            io.FontAtlas.ClearTexData();
        }

        public void NewFrame(GameTime gameTime)
        {
            var io = ImGui.GetIO();
            io.DeltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            UpdateImGuiInput(io);

            var pp = _graphicsDevice.PresentationParameters;
            io.DisplaySize = new Vector2(pp.BackBufferWidth, pp.BackBufferHeight);
            io.DisplayFramebufferScale = new Vector2(_scaleFactor);

            ImGui.NewFrame();
        }

        public void EndFrame()
        {
            ImGui.Render();
        }

        public unsafe void Draw()
        {
            var data = ImGui.GetDrawData();
            RenderImDrawData(data);
        }

        private void UpdateImGuiInput(IO io)
        {
            var kbState = Keyboard.GetState();
            UpdateKeys(_prevKeyState, kbState);

            var mouseState = Mouse.GetState();

            var windowPoint = mouseState.Position;
            io.MousePosition = new Vector2(windowPoint.X / io.DisplayFramebufferScale.X, windowPoint.Y / io.DisplayFramebufferScale.Y);

            io.MouseDown[0] = mouseState.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouseState.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouseState.MiddleButton == ButtonState.Pressed;

            float newWheelPos = mouseState.ScrollWheelValue;
            var delta = newWheelPos - _wheelPosition;
            _wheelPosition = newWheelPos;
            io.MouseWheel = delta;

            _prevKeyState = kbState;
        }

        private unsafe void RenderImDrawData(DrawData* drawData)
        {
            if (drawData == null)
            {
                Console.WriteLine("No frame rendered for ImGUI.NET, but draw was called.");
                return;
            }

            var pp = _graphicsDevice.PresentationParameters;
            // Rendering
            var displayW = pp.BackBufferWidth;
            var displayH = pp.BackBufferHeight;

            var clearColor = new Vector4(114f / 255f, 144f / 255f, 154f / 255f, 1.0f);
            _graphicsDevice.Viewport = new Viewport(0, 0, displayW, displayH);
            _graphicsDevice.Clear(new Color(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W));

            // We are using the OpenGL fixed pipeline to make the example code simpler to read!
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.RasterizerState = _rasterizerState;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            var io = ImGui.GetIO();
            ImGui.ScaleClipRects(drawData, io.DisplayFramebufferScale);

            // Render command lists

            for (var n = 0; n < drawData->CmdListsCount; n++)
            {
                var cmdList = drawData->CmdLists[n];
                var vtxBuffer = (DrawVert*)cmdList->VtxBuffer.Data;
                var idxBuffer = (ushort*)cmdList->IdxBuffer.Data;

                var vertices = new VertexPositionColorTexture[cmdList->VtxBuffer.Size];
                for (var i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new VertexPositionColorTexture(
                        new Microsoft.Xna.Framework.Vector3(vtxBuffer[i].pos.X, vtxBuffer[i].pos.Y, 0),
                        new Color(vtxBuffer[i].col),
                        new Microsoft.Xna.Framework.Vector2(vtxBuffer[i].uv.X, vtxBuffer[i].uv.Y));
                }

                _spriteEffect.CurrentTechnique.Passes[0].Apply();

                for (var cmdI = 0; cmdI < cmdList->CmdBuffer.Size; cmdI++)
                {
                    var pcmd = &(((DrawCmd*)cmdList->CmdBuffer.Data)[cmdI]);
                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _graphicsDevice.Textures[0] = _textures[pcmd->TextureId.ToInt32()];
                        _graphicsDevice.ScissorRectangle = new Rectangle(
                            (int)pcmd->ClipRect.X,
                            (int)pcmd->ClipRect.Y,
                            (int)(pcmd->ClipRect.Z - pcmd->ClipRect.X),
                            (int)(pcmd->ClipRect.W - pcmd->ClipRect.Y));
                        var indices = new short[pcmd->ElemCount];
                        for (var i = 0; i < indices.Length; i++) { indices[i] = (short) idxBuffer[i]; }
                        _graphicsDevice.DrawUserIndexedPrimitives(
                            PrimitiveType.TriangleList, vertices, 0,
                            vertices.Length, indices, 0, (int) pcmd->ElemCount / 3);
                    }
                    idxBuffer += pcmd->ElemCount;
                }
            }
        }

        public void Dispose()
        {
            _rasterizerState.Dispose();
            _spriteEffect.Dispose();
        }
    }
}
