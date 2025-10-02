using Raylib_cs;
using static Raylib_cs.Raylib;

namespace MainGame
{
    internal static class Window{   // 値だけをもち、インスタンスかしないならstructよりclass
        public const float Width = 800;
        public const float Height = 600;
    }

    internal class Paddle
    {
        private float _xPos = 0;    
        private float _yPos = 500;
        private float _width = 0;
        private float _height = 0;
        private float _slideSpeed = 9;
        public float XPos => _xPos;
        public float YPos => _yPos;
        public float Width => _width;
        public float Height => _height;

        /// <param name="width">Paddle width</param>
        /// <param name="height">Paddle height</param>
        public Paddle(float width, float height){
            _xPos= Window.Width/2;
            _width = width;
            _height = height;
            DrawRectangle((int)_xPos, (int)YPos, (int)_width, (int)_height, Color.White);
        }

        // メインループで呼び出す
        public void Update()
        {
            // 移動処理
            if (Raylib.IsKeyDown(KeyboardKey.Left)) { 
                _xPos -= _slideSpeed;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Right)) { 
                _xPos += _slideSpeed;
            }
            // クランプ (注意：rectangle は左上の点が原点）
            float leftSidePos = _xPos;
            float rightSidePos = _xPos + _width;
            if(leftSidePos < 0) 
            {
                _xPos = 0;
            }
            else if(rightSidePos > Window.Width)
            {
                _xPos = Window.Width - _width;
            }

            // 描画
            DrawRectangle((int)_xPos, (int)_yPos, (int)_width, (int)_height, Color.White);
        }
    }
    internal class Program
    {
        public static void Main(string[] args)
        {
            InitWindow((int)Window.Width, (int)Window.Height, "ブロック崩し");
            SetTargetFPS(60);
            var paddle = new Paddle(90,30);

            // 2. メインループ
            while (!WindowShouldClose())
            {
                // 更新処理（ゲームロジック）
                // ここに当たり判定や移動処理を書く
                
                // 描画処理
                BeginDrawing();
                ClearBackground(Color.Black);
                
                
                DrawText("Hello!", 100, 100, 20, Color.White);
                paddle.Update();
                EndDrawing();
            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}