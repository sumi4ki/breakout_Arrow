using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

// 衝突の検知方法の実装の仕方でブランチを分ければいいのでは？？？？
// is/asがメイン。collisionInfo, compareTag, の二つはサブブランチで
namespace MainGame
{
    internal static class Window
    {   // 値だけをもち、インスタンス化しないならstructよりclass
        public const int Width = 800;
        public const int Height = 600;
    }

    /*------------------------------------*/
    /************ Rectangle ************/
    /*------------------------------------*/
    internal class Rectangle(Vector2 position, int width, int height, Color color)  // primary constructor
    {
        private Color _color = color;
        protected Vector2 _position = position;
        protected int _width = width;
        protected int _height = height;
        public Vector2 Position => _position;
        public int Width => _width;
        public int Height => _height;

        public void Draw()
        {
            // 描画
            DrawRectangle((int)_position.X, (int)_position.Y, _width, _height, _color);
        }
    }

    /*------------------------------------*/
    /************ Paddle ************/
    /*------------------------------------*/
    internal class Paddle : Rectangle
    {
        private float _slideSpeed = 9;

        public Paddle() : base(new(Window.Width, 500), 120, 20, Color.White)
        {
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
        }
    }

    /*------------------------------------*/
    /************ Ball ************/
    /*------------------------------------*/
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
            // 物理更新のみ
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
        }
        public void Draw()
        {
            DrawCircle((int)_position.X, (int)_position.Y, _radius, Color.Beige);
        }
        public void OnCollisionEnter() { }
    }

    /*------------------------------------*/
    /************ CollisionManager ************/
    /*------------------------------------*/
    internal class CollisionManager
    {
        readonly Ball _ball;
        readonly Paddle _paddle;

        public CollisionManager(Ball b, Paddle p)
        {
            _ball = b;
            _paddle = p;
        }

        public void Update()
        {
            if (BallAndRectCollisionCheck(_ball, _paddle))
            {
                _ball.OnCollisionEnter();
            }
        }

        // Ball クラスと Rectangle クラスの衝突
        // 座標更新が行われた後に呼び出す。次フレームの座標で計算するため。
        public static bool BallAndRectCollisionCheck(Ball ball, Rectangle rect)
        {
            if (ball == null || rect == null) return false;
            Vector2 intersect = new(0, 0);  // ボールと長方形の最近点を求める

            // 最近点のX座標
            // ball.x が rect より左にある時
            if (ball.Position.X < rect.Position.X)
            {
                intersect.X = rect.Position.X; // 最近点のX座標は rect の左辺
            }
            // ball.x が rect より右にある時
            else if (ball.Position.X > rect.Position.X + rect.Width)
            {
                intersect.X = rect.Position.X + rect.Width;   // 最近点のX座標は rect の右辺
            }
            // ball.x が rect の内側
            else
            {
                intersect.X = ball.Position.X;
            }

            // 最近点のY座標
            // ball.y が rect より上にある時
            if (ball.Position.Y < rect.Position.Y)
            {
                intersect.Y = rect.Position.Y; // 最近点のY座標は rect の上辺
            }
            // ball.y が rect より下にある時
            else if (ball.Position.Y > rect.Position.Y + rect.Height)
            {
                intersect.Y = rect.Position.Y + rect.Height;    // 最近点のY座標は rect の底辺
            }
            // ball.y が rect より内側にある時
            else
            {
                intersect.Y = ball.Position.Y;
            }

            // 求めた最近点とボールの中心座標で距離を計算して、それが半径より小さければ衝突
            double distanseSqr = Math.Pow(ball.Position.X - intersect.X, 2) +
                                    Math.Pow(ball.Position.Y - intersect.Y, 2);

            if (distanseSqr < Math.Pow(ball.Radius, 2))
            {
                Console.WriteLine($"Ball{ball.Position}: collides Block{rect.Position}");
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    /*-----------------------------------*/
    /************ main program *************/
    /*-----------------------------------*/
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
                // 座標更新
                ball.Update();
                paddle.Update();

                // 物理処理（座標の更新はこれより前に済ませる）
                collisionManager.Update();

                // 描画処理
                BeginDrawing();
                ClearBackground(Color.Black);

                DrawText("Hello!", 100, 100, 20, Color.White);
                ball.Draw();
                paddle.Draw();

                EndDrawing();

            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}