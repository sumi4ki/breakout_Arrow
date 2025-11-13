using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Color = Raylib_cs.Color;

// ブランチを変えたからここからスタート

// 衝突の検知方法の実装の仕方でブランチを分ければいいのでは？？？？
// is/asがメイン。collisionInfo, compareTag, の二つはサブブランチで
namespace breakout_game
{
    internal static class Window
    {   // 値だけをもち、インスタンス化しないならstructよりclass
        public const int Width = 800;
        public const int Height = 600;
    }

    internal interface ICollidable
    {
        public bool IsActive { get; }
        void OnCollisionEnter(CollisionInfo other);
    }

    internal struct CollisionInfo
    {
        internal ICollidable Other { get; set; }
        // internal Vector2 Normal { get; set; }　// 必要になったら実装
        internal Vector2 Point { get; set; }
    }


    /*------------------------------------*/
    /************ Rectangle ************/
    /*------------------------------------*/
    internal class Rectangle
    {
        protected Vector2 _position;
        protected int _width;
        protected int _height;
        private Color _color;

        public Rectangle(Vector2 position, int width, int height, Color color)
        {
            _position = position;
            _width = width;
            _height = height;
            _color = color;
        }

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
    internal class Paddle : Rectangle, ICollidable
    {
        private Vector2 _nextPosition;
        public Vector2 NextPosition => _nextPosition;
        private readonly float _slideSpeed = 9;
        public float SlideSpeed => _slideSpeed;
        public bool IsActive { get; private set; } = true;

        public Paddle() : base(new(Window.Width / 2+ 120/2, 500), 120, 20, Color.White)
        {
        }

        // GameManagerのUpdateで呼び出さないと、Paddle が移動しない。
        public void ComputeNextPosition()
        {
            _nextPosition = _position;
            // 移動処理
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                _nextPosition.X -= _slideSpeed;
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                _nextPosition.X += _slideSpeed;
            }
            // windowからはみ出ないようにする (注意：rectangle は左上の点が原点）
            float leftSidePos = _nextPosition.X;
            float rightSidePos = _nextPosition.X + _width;
            if (leftSidePos < 0)
            {
                _nextPosition.X = 0;
            }
            else if (rightSidePos > Window.Width)
            {
                _nextPosition.X = Window.Width - _width;
            }
        }
        public void ApplyNextPosition()
        {
            _position = _nextPosition;
        }
        // メインループで呼び出す
        public void Update()
        {
            

        }

        public void OnCollisionEnter(CollisionInfo info)
        {
            // 今のところパドル側で衝突時の処理はない
        }
    }

    public enum BlockType
    {
        Destructible, // 壊れるブロック
        Wall          // 壁（壊れない）
    }

    public struct BlockGridConfig
    {
        public int Rows;
        public int Cols;
        public int BlockWidth;
        public int BlockHeight;
        public int StartX;
        public int StartY;
        public int Padding;
    }

    /*--------------------------------*/
    /************ Block ************/
    /*--------------------------------*/
    // internal class Block : Rectangle, ICollidable
    // {

    //     protected BlockType _type;
    //     public BlockType Type => _type;
    //     protected bool _isActive = true;
    //     public Block() : base(new(Window.Width, 500), 120, 20, Color.White)
    //     {
    //     }
    //     public void OnCollisionEnter(CollisionInfo info)
    //     {
    //         // 今回は未実装
    //     }
    // }
    internal abstract class Block : Rectangle, ICollidable
    {
        public bool IsActive { get; protected set; } = true;
        public BlockType Type { get; protected set; }

        public Block(Vector2 position, int width, int height, Color color) : base(position, width, height, color)
        {
            ;
        }
        // 衝突時の動作：派生クラスごとに変える想定
        public abstract void OnCollisionEnter(CollisionInfo other);
    }

    internal class WallBlock(Vector2 position, int width, int height, Color color)
                                : Block(position, width, height, color)
    {
        public override void OnCollisionEnter(CollisionInfo info)
        {
            // 壊れないので何もしない
        }

    }
    internal class DeconstructibleBlock(Vector2 position, int width, int height, Color color)
                                : Block(position, width, height, color)
    {
        public override void OnCollisionEnter(CollisionInfo info)
        {
            // 壊れる処理をここに実装
            IsActive = false;
        }

    }

    //あとGameManagerも別で作る予定
    internal class BlockManager
    {
        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks => _blocks;
        public void CreateGridBlocks(BlockGridConfig config)
        {
            for (int row = 0; row < config.Rows; row++)
            {
                for (int col = 0; col < config.Cols; col++)
                {
                    int x = config.StartX + col * (config.BlockWidth + config.Padding);
                    int y = config.StartY + row * (config.BlockHeight + config.Padding);
                    string type;
                    // // 境界条件でブロックタイプを決める
                    // if (row == 0 || row == rows - 1 || col == 0 || col == cols - 1)
                    // {
                    //     // type = "wall";
                    // }
                    // else
                    // {
                    //     type = "deconstructible";
                    // }
                    // 種類が決まったらファクトリに任せる
                    type = "deconstructible"; // とりあえず全部壊せるブロックで。種類が増えたら変更
                    Block block = BlockFactory.CreateBlock(type, new Vector2(x, y), config.BlockWidth, config.BlockHeight, Color.White);
                    _blocks.Add(block);
                }
            }
        }
        
        public void CreateBlocksFromPositions(List<(Vector2 position, int width, int height, string type)> blockParams)
        {
            foreach (var param in blockParams)
            {
                Block b = BlockFactory.CreateBlock(param.type, param.position, param.width, param.height, Color.Gray);
                _blocks.Add(b);
            }
        }

        /*------------------------------------*/
        /************ BlockFactory ************/
        /*------------------------------------*/
        // Factoryパターンの実装例 （静的クラス）。Blockの生成のみ担当。条件分岐は呼び出し元で行う。
        internal static class BlockFactory
        {
            public static Block CreateBlock(string type, Vector2 position, int width, int height, Color color)
            {
                return type switch
                {
                    "wall" => new WallBlock(position, width, height, color),
                    "deconstructible" => new DeconstructibleBlock(position, width, height, color),
                    _ => throw new ArgumentException($"Unknown block type: {type}")
                };
            }
        }

        public void DrawBlocks()
        {
            foreach (var block in _blocks)
            {
                if (block.IsActive)
                {
                    block.Draw();
                }

            }
        }
    }

    /*------------------------------------*/
    /************ Ball ************/
    /*------------------------------------*/
    internal class Ball : ICollidable
    {
        private float _radius = 15;
        public float Radius => _radius;
        private Vector2 _position = new(0, 0);
        public Vector2 Position => _position;
        private Vector2 _nextFramePosition = new(0, 0);
        public Vector2 NextFramePosition => _nextFramePosition;
        public bool IsActive { get; private set; } = true;

        private Vector2 _speed = new(10, 10);
        private Vector2 _dir = new(0, 0);
        public Vector2 Direction => _dir; // 読み取り専用
        public float maxBounceAngle = 75f;
        public void SetDirectionFromAngle(float angleRadians)
        {
            _dir.X = MathF.Cos(angleRadians);
            _dir.Y = MathF.Sin(angleRadians);
        }

        /// <summary>
        /// 角度（度数法）から方向ベクトルを設定する
        /// 真上方向を0度、右回りに角度が増えるものとする
        /// </summary>
        /// <param name="angleDegrees"></param>
        public void SetDirectionFromAngleDegrees(float angleDegrees)
        {
            // x軸は右向き。y軸は下向きだから、270度は真上。上向き基準に変換
            float angleRadians = (angleDegrees+270) * MathF.PI / 180f;
            Console.WriteLine("angleRadians: " + angleRadians + " for angleDegrees: " + angleDegrees+270);
            _dir.X = MathF.Cos(angleRadians);
            _dir.Y = MathF.Sin(angleRadians);
        }

        public Ball(int initXPos, int initYPos)
        {
            SetDirectionFromAngle(MathF.PI / 2); // 45度上向き
            _position = new Vector2(initXPos, initYPos);
        }

        public void ComputeNextPosition()
        {

            _nextFramePosition = _position + new Vector2(_speed.X * _dir.X, _speed.Y * _dir.Y);
        }

        public void ResolveWallCollision(int windowWidth, int windowHeight)
        {
            if (_nextFramePosition.X - _radius < 0)
            {
                _dir.X *= -1;
                _nextFramePosition.X = _radius;
            }
            else if (_nextFramePosition.X + _radius > windowWidth)
            {
                _dir.X *= -1;
                _nextFramePosition.X = windowWidth - _radius;
            }

            if (_nextFramePosition.Y - _radius < 0)
            {
                _dir.Y *= -1;
                _nextFramePosition.Y = _radius;
            }
            else if (_nextFramePosition.Y + _radius > windowHeight)
            {
                _dir.Y *= -1;
                _nextFramePosition.Y = windowHeight - _radius;
            }
        }

        public void ApplyNextPosition()
        {
            _position = _nextFramePosition;
        }

        public void Update()
        {
            // _nextFramePosition = new Vector2(_position.X + _speed.X * Direction.X
            //                         , _position.Y + _speed.Y * Direction.Y);
            // // windowからはみ出さないようにする
            // if (_nextFramePosition.X - _radius < 0 || _nextFramePosition.X + _radius > Window.Width)
            // {
            //     _dir.X *= -1;
            // }
            // else if (_nextFramePosition.Y - _radius < 0 || _nextFramePosition.Y + _radius > Window.Height)
            // {
            //     _dir.Y *= -1;
            // }

            // _position = _nextFramePosition;
            ComputeNextPosition();
        }

        public void Draw()
        {
            DrawCircle((int)_position.X, (int)_position.Y, _radius, Color.Beige);
        }

        public void OnCollisionEnter(CollisionInfo info)
        {
            if (info.Other is Paddle pd)
            {
                // info.Point と Paddle の中心のX座標の差を求める. マイナスなら中心より左、プラスなら右
                float diffX = (pd.Position.X + (pd.Width / 2)) - _position.X;
                diffX *= -1; // 右方向を正にするために反転 TODO: なぜ反転が必要なのか理解する
                // その差を Paddle の幅で割って、-1 ～ 1 の範囲に正規化する
                float normalizedDiffX = diffX / (pd.Width / 2);
                float bounceAngle = normalizedDiffX * maxBounceAngle;
                SetDirectionFromAngleDegrees(bounceAngle);
                ComputeNextPosition(); // 衝突後の次フレーム位置を再計算
            }
            if (info.Other is Block rect)
            {
                // 未実装
            }
        }
    }

    /*------------------------------------*/
    /************ CollisionManager ************/
    /*------------------------------------*/
    internal class CollisionManager
    {
        // Constructor-injected dependencies (non-nullable)
        private readonly Ball _ball;
        private readonly Paddle _paddle;
        private readonly List<Block> _blocks;

        public CollisionManager(Ball ball, Paddle paddle, List<Block> blocks)
        {
            _ball = ball ?? throw new ArgumentNullException(nameof(ball));
            _paddle = paddle ?? throw new ArgumentNullException(nameof(paddle));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }

        public void Update()
        {
            // Ball vs Paddle
            if (BallRectCollisionCheck(_ball.NextFramePosition, _ball.Radius,
                                        _paddle.NextPosition, _paddle.Width, _paddle.Height))
            {
                CollisionInfo info = new()
                {
                    Other = _paddle,
                    Point = _paddle.Position,
                };
                _ball.OnCollisionEnter(info);
                // paddle 側の処理も呼び出す
                CollisionInfo info2 = new()
                {
                    Other = _ball,
                    Point = _ball.Position,
                };
                _paddle.OnCollisionEnter(info2);
            }

            // Ball vs Blocks
            foreach (var block in _blocks)
            {
                if (BallRectCollisionCheck(_ball.NextFramePosition, _ball.Radius,
                                            block.Position, block.Width, block.Height))
                {
                    CollisionInfo info = new()
                    {
                        Other = block,
                        Point = block.Position,
                    };
                    _ball.OnCollisionEnter(info);
                    // block 側の処理も呼び出す
                    CollisionInfo info2 = new()
                    {
                        Other = _ball,
                        Point = _ball.Position,
                    };
                    block.OnCollisionEnter(info2);
                }
            }
        }

        // Ball クラスと Rectangle クラスの衝突
        // 座標更新が行われた後に呼び出す。次フレームの座標で計算するため。
        // ダメだったら、git checkout で戻す。
        public static bool BallRectCollisionCheck(Vector2 ballPos, float ballRadius,
                                                    Vector2 rectPos, int rectWidth, int rectHeight)
        {
            Vector2 intersect = new(0, 0);  // ボールと長方形の最近点を求める

            // 最近点のX座標
            // ball.x が rect より左にある時
            if (ballPos.X < rectPos.X)
            {
                intersect.X = rectPos.X; // 最近点のX座標は rect の左辺
            }
            // ball.x が rect より右にある時
            else if (ballPos.X > rectPos.X + rectWidth)
            {
                intersect.X = rectPos.X + rectWidth;   // 最近点のX座標は rect の右辺
            }
            // ball.x が rect の内側
            else
            {
                intersect.X = ballPos.X;
            }

            // 最近点のY座標
            // ball.y が rect より上にある時
            if (ballPos.Y < rectPos.Y)
            {
                intersect.Y = rectPos.Y; // 最近点のY座標は rect の上辺
            }
            // ball.y が rect より下にある時
            else if (ballPos.Y > rectPos.Y + rectHeight)
            {
                intersect.Y = rectPos.Y + rectHeight;    // 最近点のY座標は rect の底辺
            }
            // ball.y が rect より内側にある時
            else
            {
                intersect.Y = ballPos.Y;
            }

            // 求めた最近点とボールの中心座標で距離を計算して、それが半径より小さければ衝突
            double distanseSqr = Math.Pow(ballPos.X - intersect.X, 2) +
                                    Math.Pow(ballPos.Y - intersect.Y, 2);

            if (distanseSqr < Math.Pow(ballRadius, 2))
            {
                // Console.WriteLine($"Ball{ball.Position}: collides Block{rect.Position}");
                return true;
            }
            else
            {
                return false;
            }
        } 
    }

    /*-----------------------------------*/
    /************ GameManager*************/
    /*-----------------------------------*/
    // ゲーム初期化とループの処理を追加する.
    internal class GameManager
    {
        private BlockManager _blockManager;
        private CollisionManager _collisionManager;
        private Ball _ball;
        private Paddle _paddle;
    
        public GameManager()
        {
            _blockManager = new BlockManager();
    
            _paddle = new Paddle();
            _ball = new Ball(Window.Width / 2, Window.Height / 2);
    
            var config = new BlockGridConfig {
                Rows = 5,
                Cols = 10,
                BlockWidth = 60,
                BlockHeight = 20,
                StartX = 50,
                StartY = 50,
                Padding = 5
            };
            _blockManager.CreateGridBlocks(config);
    
            var wallThickness = 30;
            var wallParams = new List<(Vector2 position, int width, int height, string type)>
            {
                (new Vector2(-wallThickness, wallThickness), Window.Width + wallThickness * 2, wallThickness, "wall"),
                (new Vector2(-wallThickness, 0), wallThickness, Window.Height, "wall"),
                (new Vector2(Window.Width, 0), wallThickness, Window.Height, "wall"),
                (new Vector2(-wallThickness, Window.Height), Window.Width + wallThickness * 2, wallThickness, "wall"),
            };
            _blockManager.CreateBlocksFromPositions(wallParams);
    
            _collisionManager = new CollisionManager(_ball, _paddle, _blockManager.Blocks);
    
            Console.WriteLine("Game initialized!");
        }
    
        public void Update()
        {
            // 次フレームの座標計算
            _ball.ComputeNextPosition();
            _paddle.ComputeNextPosition();
            
            // 衝突判定
            _collisionManager.Update();
            _ball.ResolveWallCollision(Window.Width, Window.Height);
            
            // 座標適用
            _ball.ApplyNextPosition();
            _paddle.ApplyNextPosition();

            // 描画
            _blockManager.DrawBlocks();
            _ball.Draw();
            _paddle.Draw();
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

            var gameManager = new GameManager();

            // 2. メインループ
            while (!WindowShouldClose())
            {
                // 更新処理（ゲームロジック）
                // 座標更新
                // ball.Update();
                // paddle.Update();

                // 物理処理
                gameManager.Update();
                // 応急処置で壁の判定はcollisionMnager .Update()の後に呼ぶようにする。
                // ResolveWallCollision内で_nextFramePositionを書き換えてしまうため。

                // ball.ApplyNextPosition();

                // 描画処理
                BeginDrawing();
                ClearBackground(Color.Black);

                // DrawText("Hello!", 100, 100, 20, Color.White);
                // ball.Draw();
                // paddle.Draw();

                EndDrawing();

            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}