using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace ImGuiNET.MonoGame
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private ImGuiMg _imgui;

        private int _pressCount;
        private Vector4 _buttonColor = new Vector4(55f / 255f, 155f / 255f, 1f, 1f);
        private bool _mainWindowOpened;
        private float _sliderVal;
        private Vector3 _positionValue = new Vector3(500);
        public readonly IntPtr TextInputBuffer;
        public readonly int TextInputBufferLength;

        public unsafe Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = false;

            _graphics.PreferredBackBufferWidth = 960;
            _graphics.PreferredBackBufferHeight = 540;

            TextInputBufferLength = 1024;
            TextInputBuffer = Marshal.AllocHGlobal(TextInputBufferLength);
            var ptr = (long*)TextInputBuffer.ToPointer();
            for (var i = 0; i < 1024 / sizeof(long); i++)
                ptr[i] = 0;
        }

        protected override void LoadContent()
        {
            _imgui = new ImGuiMg(Window, GraphicsDevice);

            /*var frc = new FrameRateCounter(this);
            frc.Position = new Microsoft.Xna.Framework.Vector2(10f, 500f);
            frc.Font = Content.Load<SpriteFont>("font");
            Components.Add(frc);*/
        }

        protected override void Dispose(bool disposing)
        {
            _imgui.Dispose();
            base.Dispose(disposing);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _imgui.NewFrame(gameTime);
            SubmitImGuiStuff();
            _imgui.EndFrame();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _imgui.Draw();

            base.Draw(gameTime);
        }

        private unsafe void SubmitImGuiStuff()
        {
            ImGui.GetStyle().WindowRounding = 0;

            var pp = GraphicsDevice.PresentationParameters;
            ImGui.SetNextWindowSize(new Vector2(pp.BackBufferWidth - 20, pp.BackBufferHeight - 50), Condition.Once);
            //ImGui.SetNextWindowPosCenter(SetCondition.Always);

            ImGui.BeginWindow("Hello from ImGui!", ref _mainWindowOpened, WindowFlags.Default);

            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("About", "Ctrl-Alt-A", false, true))
                {
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();


            var pos = ImGui.GetIO().MousePosition;
            var leftPressed = ImGui.GetIO().MouseDown[0];
            ImGui.Text("Current mouse position: " + pos + ". Pressed=" + leftPressed);

            if (ImGui.Button("Increment the counter."))
                _pressCount += 1;

            ImGui.Text($"Button pressed {_pressCount} times.", new Vector4(0, 1, 1, 1));

            ImGui.Text("Input some text:");
            ImGui.InputTextMultiline(string.Empty,
                TextInputBuffer, (uint) TextInputBufferLength,
                new Vector2(360, 240),
                InputTextFlags.Default,
                OnTextEdited);

            ImGui.SliderFloat("SlidableValue", ref _sliderVal, -50f, 100f, $"{_sliderVal:##0.00}", 1.0f);
            ImGui.DragVector3("Vector3", ref _positionValue, -100, 100);

            if (ImGui.TreeNode("First Item"))
            {
                ImGui.Text("Word!");
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("Second Item"))
            {
                ImGui.ColorButton("ColorChangeButton", _buttonColor, ColorEditFlags.Default, new Vector2(20));
                if (ImGui.Button("Push me to change color", new Vector2(120, 30)))
                {
                    _buttonColor = new Vector4(_buttonColor.Y + .25f, _buttonColor.Z, _buttonColor.X, _buttonColor.W);
                    if (_buttonColor.X > 1.0f)
                    {
                        _buttonColor.X -= 1.0f;
                    }
                }

                ImGui.TreePop();
            }

            if (ImGui.Button("Press me!", new Vector2(100, 30)))
                ImGuiNative.igOpenPopup("SmallButtonPopup");

            if (ImGui.BeginPopup("SmallButtonPopup"))
            {
                ImGui.Text("Here's a popup menu.");
                ImGui.Text("With two lines.");

                ImGui.EndPopup();
            }

            if (ImGui.Button("Open Modal window"))
                ImGui.OpenPopup("ModalPopup");
            if (ImGui.BeginPopupModal("ModalPopup"))
            {
                ImGui.Text("You can't press on anything else right now.");
                ImGui.Text("You are stuck here.");
                if (ImGui.Button("OK", new Vector2(0, 0))) { }
                ImGui.SameLine();
                ImGui.Dummy(100f, 0f);
                ImGui.SameLine();
                if (ImGui.Button("Please go away", new Vector2(0, 0))) { ImGui.CloseCurrentPopup(); }

                ImGui.EndPopup();
            }

            ImGui.Text("I have a context menu.");
            if (ImGui.BeginPopupContextItem("ItemContextMenu"))
            {
                if (ImGui.Selectable("How's this for a great menu?")) { }
                ImGui.Selectable("Just click somewhere to get rid of me.");
                ImGui.EndPopup();
            }

            ImGui.EndWindow();
        }

        public unsafe int OnTextEdited(TextEditCallbackData* data)
        {
            var currentEventChar = (char)data->EventChar;
            return 0;
        }
    }
}
