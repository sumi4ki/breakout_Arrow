using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace MainGame
{
    internal static class Window{   // 値だけをもち、インスタンス化しないならstructよりclass
        public const int Width = 800;
        public const int Height = 600;
    }

    internal class Rectangle
    {
        protected Vector2 _position = new(0, 0);
        protected int _width = 0;
        protected int _height = 0;
        public Vector2 Position => _position;
        public int Width => _width;
        public int Height => _height;

        public Rectangle(Vector2 position, int width, int height)
        {
            _position = position;
            _width = width;
            _height = height;
        }
    }
    
    internal class Paddle : Rectangle
    {
        private float _slideSpeed = 9;

        public Paddle() : base(new(Window.Width, 500), 120, 20)
        {
            ;
        }

        // メインループで呼び出す
        public void Update()
        {
            // 移動処理
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                _position.X -= _slideSpeed;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                _position.X += _slideSpeed;
            }
            // windowからはみ出ないようにする (注意：rectangle は左上の点が原点）
            float leftSidePos = _position.X;
            float rightSidePos = _position.X + _width;
            if (leftSidePos < 0)
            {
                _position.X = 0;
            }
            else if (rightSidePos > Window.Width)
            {
                _position.X = Window.Width - _width;
            }

            // 描画
            DrawRectangle((int)_position.X, (int)_position.Y, _width, _height, Color.White);
        }
    }

    internal class Ball
    {
        private float _radius = 15;
        private Vector2 _position = new(0, 0);
        public Vector2 Position => _position;
        private Vector2 _speed = new(5, 5);
        private Vector2 _dir = new(0, 0);
        public float Radius => _radius;
        public Vector2 Direction
        {
            get => _dir;
            set
            {
                if (Math.Abs(value.X) > 1 || Math.Abs(value.Y) > 1)
                {
                    throw new ArgumentException("Directionの成分は絶対値1より小さくする");
                }
                else
                {
                    _dir.X = value.X;
                    _dir.Y = value.Y;
                }
            }
        }

        public Ball(int initXPos, int initYPos)
        {
            Direction = new(1, -1); // 最初は右上に飛ばす
            _position = new Vector2(initXPos, initYPos);
        }
        public void Update()
        {

            Vector2 nextFramePos = new(_position.X + _speed.X * Direction.X
                                    , _position.Y + _speed.Y * Direction.Y);

            // windowからはみ出さないようにする
            if (nextFramePos.X - _radius < 0 || nextFramePos.X + _radius > Window.Width)
            {
                _dir.X *= -1;
            }
            else if (nextFramePos.Y - _radius < 0 || nextFramePos.Y + _radius > Window.Height)
            {
                _dir.Y *= -1;
            }

            _position = nextFramePos;
            DrawCircle((int)_position.X, (int)_position.Y, _radius, Color.Beige);
        }
        public void OnCollisionEnter() {}
    }

    internal class CollisionManager
    {
        Ball _ball;
        Paddle _paddle;

        public CollisionManager(Ball b, Paddle p)
        {
            _ball = b;
            _paddle = p;
        }

        public void Update()
        {
            BallAndBlockCollisionCheck(_ball, _paddle);
        }

        // Ball クラスと Rectangle クラスの衝突
        public bool BallAndBlockCollisionCheck(Ball ball, Rectangle block)
        {
            if (ball == null || block == null) return false;
            Vector2 intersect = new(0, 0);  // ボールと長方形の最近点を求める

            // 最近点のX座標
            // ball.x がブロックより左にある時
            if (ball.Position.X < block.Position.X)
            {
                intersect.X = block.Position.X; // 最近点のX座標は block の左辺
            }
            // ball.x がブロックより右にある時
            else if (ball.Position.X > block.Position.X + block.Width)
            {
                intersect.X = block.Position.X + block.Width;   // 最近点のX座標は block の右辺
            }
            // ball.x がブロックの内側
            else
            {
                intersect.X = ball.Position.X;
            }

            // 最近点のY座標
            // ball.y がブロックより上にある時
            if (ball.Position.Y < block.Position.Y)
            {
                intersect.Y = block.Position.Y; // 最近点のY座標は block の上辺
            }
            // ball.y がブロックより下にある時
            else if (ball.Position.Y > block.Position.Y + block.Height)
            {
                intersect.Y = block.Position.Y + block.Height;    // 最近点のY座標は block の底辺
            }
            // ball.y がブロックより内側にある時
            else
            {
                intersect.Y = ball.Position.Y;
            }

            // 求めた最近点とボールの中心座標で距離を計算して、それが半径より小さければ衝突
            double distanseSqr = Math.Pow(ball.Position.X - intersect.X, 2) +
                                    Math.Pow(ball.Position.Y - intersect.Y, 2);

            if (distanseSqr < Math.Pow(ball.Radius, 2))
            {
                Console.WriteLine($"Ball{ball.Position}: collides Block{block.Position}");
                return true;
            }
            else
            {
                return false;
            }
        }
        
    }
    internal class Program
    {
        public static void Main(string[] args)
        {
            InitWindow(Window.Width, Window.Height, "ブロック崩し");
            SetTargetFPS(60);

            var paddle = new Paddle();
            var ball = new Ball(Window.Width / 2, Window.Height / 2);
            var collisionManager = new CollisionManager(ball, paddle);

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
                collisionManager.Update();

                EndDrawing();
            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}