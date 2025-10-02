using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace MainGame
{
    internal static class Window{   // 値だけをもち、インスタンスかしないならstructよりclass
        public const int Width = 800;
        public const int Height = 600;
    }

    internal class Paddle
    {
        private float _xPos = 0;    
        private float _yPos = 500;
        private int _width = 0;
        private int _height = 0;
        private float _slideSpeed = 9;
        public float XPos => _xPos;
        public float YPos => _yPos;
        public int Width => _width;
        public int Height => _height;

        /// <param name="width">Paddle width</param>
        /// <param name="height">Paddle height</param>
        public Paddle(int width, int height){
            _xPos= Window.Width/2;
            _width = width;
            _height = height;
            DrawRectangle((int)_xPos, (int)YPos, _width, _height, Color.White);
        }
        
        // メインループで呼び出す
        public void Update()
        {
            // 移動処理
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                _xPos -= _slideSpeed;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                _xPos += _slideSpeed;
            }
            // クランプ (注意：rectangle は左上の点が原点）
            float leftSidePos = _xPos;
            float rightSidePos = _xPos + _width;
            if (leftSidePos < 0)
            {
                _xPos = 0;
            }
            else if (rightSidePos > Window.Width)
            {
                _xPos = Window.Width - _width;
            }

            // 描画
            DrawRectangle((int)_xPos, (int)_yPos, _width, _height, Color.White);
        }
    }

    internal class Ball
    {
        private float _radius = 15;
        private float _xPos = 0;
        private float _yPos = 0;
        private Vector2 _speed = new(5, 5);
        private Vector2 _dir = new(0, 0);
        public float Radius => _radius;
        public Vector2 Direction
        {
            get => _dir;
            set
            {
                if (value.X == 0 || value.Y == 0)
                {
                    throw new ArgumentException("0は入力できません");
                }
                else
                {
                    _dir.X = value.X > 0 ? 1 : -1;
                    _dir.Y = value.Y > 0 ? 1 : -1;
                }
            }
        }

        public Ball(int initXPos, int initYPos)
        {
            Direction = new(1, -1); // 最初は右上に飛ばす
            _xPos = initXPos;
            _yPos = initYPos;
        }
        public void Update()
        {
            // 座標更新
            _xPos += _speed.X * Direction.X;
            _yPos += _speed.Y * Direction.Y;

            DrawCircle((int)_xPos, (int)_yPos, _radius, Color.Red);
        }
    }
    internal class Program
    {
        public static void Main(string[] args)
        {
            InitWindow(Window.Width, Window.Height, "ブロック崩し");
            SetTargetFPS(60);
            var paddle = new Paddle(90, 30);
            var ball = new Ball(Window.Width/2, Window.Height/2);

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
                ball.Update();
                EndDrawing();
            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}