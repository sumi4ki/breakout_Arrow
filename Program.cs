using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
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
        internal ICollidable Object { get; set; }
        internal Vector2 ContactPoint { get; set; }
        internal float EntryTime{get; set;}
        internal Vector2 Normal{get; set;}
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

        public Paddle() : base(new(Window.Width / 2-30, 500), 120, 20, Color.White)
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
                    BlockType type;
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
                    type = BlockType.Destructible; // とりあえず全部壊せるブロックで。種類が増えたら変更
                    Block block = BlockFactory.CreateBlock(type, new Vector2(x, y), config.BlockWidth, config.BlockHeight, Color.White);
                    _blocks.Add(block);
                }
            }
        }
        
        public void CreateBlocksFromPositions(List<(Vector2 position, int width, int height, BlockType type)> blockParams)
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
            public static Block CreateBlock(BlockType type, Vector2 position, int width, int height, Color color)
            {
                return type switch
                {
                    BlockType.Wall => new WallBlock(position, width, height, color),
                    BlockType.Destructible => new DeconstructibleBlock(position, width, height, color),
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
        public Vector2 Speed => _speed;
        private Vector2 _dir = new(0, 0);
        public Vector2 Direction => _dir; // 読み取り専用
        public float maxBounceAngle = 60f;

        public Ball(int initXPos, int initYPos)
        {
            SetDirectionFromAngleDegrees(215); // 上向き
            _position = new Vector2(initXPos, initYPos+100); // 初期位置
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
            Console.WriteLine("angleRadians: " + angleRadians + " for angleDegrees: " + (angleDegrees+270));
            _dir.X = MathF.Cos(angleRadians);
            _dir.Y = MathF.Sin(angleRadians);
        }

        public void InvertYDirection()
        {
            _dir.Y *= -1;
        }
        public void InvertXDirection()
        {
            _dir.X *= -1;
        }

        public void ComputeNextPosition()
        {

            _nextFramePosition = _position + _dir*_speed;
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

        public void SetNextPosition(Vector2 pos)
        {
            _nextFramePosition = pos;
        }

        public void Update()
        {
            ComputeNextPosition();
        }

        public void Draw()
        {
            DrawCircle((int)_position.X, (int)_position.Y, _radius, Color.Beige);
        }

        public void OnCollisionEnter(CollisionInfo info)
        {
            if (info.Object is Paddle pd)
            {
                // info.Point と Paddle の中心のX座標の差を求める. マイナスなら中心より左、プラスなら右
                float diffX = (pd.Position.X + (pd.Width / 2)) - _position.X;
                diffX *= -1; // 右方向を正にするために反転 TODO: なぜ反転が必要なのか理解する
                // その差を Paddle の幅で割って、-1 ～ 1 の範囲に正規化する
                float normalizedDiffX = diffX / (pd.Width / 2);
                float bounceAngle = normalizedDiffX * maxBounceAngle;
                SetDirectionFromAngleDegrees(bounceAngle);
                ComputeNextPosition(); // 衝突後の次フレーム位置を再計算
                ApplyNextPosition();
                Console.WriteLine("Ball collides Paddle at " + _position);
            }
            if (info.Object is Block)
            {
                Console.WriteLine("Ball collides Block at " + info.Normal);
                
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

        public void BallMove()
        {
            // ball vs All 
            foreach(var block in _blocks)
            {
                if(!block.IsActive)
                {
                    continue; // 非アクティブなブロックは無視
                }
                // すでに衝突している場合、押し戻す
                BallPaddleCollisionCheck(_ball, block);
            }
   
            // Ball vs Paddle
            if (BallPaddleCollisionCheck(_ball, _paddle))
            {
                // CollisionInfo info = new()
                var infoBall = new CollisionInfo
                {
                    Object = _paddle,
                    ContactPoint = _paddle.Position,
                };
                _ball.OnCollisionEnter(infoBall);
                // paddle 側の処理も呼び出す
                var infoPaddle = new CollisionInfo()
                {
                    Object = _ball,
                    ContactPoint = _ball.Position,
                };
                _paddle.OnCollisionEnter(infoPaddle);
            }

            // Ball vs Blocks
            float remainingTime = 1.0f; // 1フレーム分の時間.padlle衝突はしない前提
            while (remainingTime > 0.01f)    // 微小時間まで繰り返す。秒数未検証
            {
                // もっとも衝突まで近いブロックを探索
                CollisionInfo targetBlockInfo = new() { EntryTime = float.PositiveInfinity };
                foreach (var block in _blocks)
                {
                    if (!block.IsActive)
                    {
                        continue; // 非アクティブなブロックは無視
                    }

                    // 残り時間で、SweptAABB判定
                    if (BallAndBlockCollision(_ball, block, remainingTime, out CollisionInfo tmpBlockInfo))
                    {
                        if (tmpBlockInfo.EntryTime < targetBlockInfo.EntryTime)
                        {
                            targetBlockInfo = tmpBlockInfo;
                        }
                    }
                }

                if (targetBlockInfo.EntryTime == float.PositiveInfinity)
                {
                    // 衝突するブロックがない場合、残り時間分移動して終了
                    _ball.SetNextPosition(_ball.Direction * _ball.Speed * remainingTime + _ball.Position);
                    _ball.ApplyNextPosition();
                    break;
                }

                // 衝突処理
                _ball.SetNextPosition(_ball.Direction * _ball.Speed * targetBlockInfo.EntryTime + _ball.Position);
                _ball.ApplyNextPosition();
                // 衝突したオブジェクトに通知 // TODO: ボールとブロックに通知する情報は分ける
                _ball.OnCollisionEnter(targetBlockInfo);
                targetBlockInfo.Object.OnCollisionEnter(targetBlockInfo);
                // 反射
                if (targetBlockInfo.Normal.X == -1.0f || targetBlockInfo.Normal.X == 1.0f)
                {
                    _ball.InvertXDirection();
                }
                else
                {
                    _ball.InvertYDirection();
                }

                remainingTime -= targetBlockInfo.EntryTime;               // まだ時間が残っている
            }
        }

        // Swept AABB 衝突判定（ball は動き、block は静止している場合）
        // return true → info.EntryTime = EntryTime
        // return false → info.EntryTime = -1.0
        public static bool BallAndBlockCollision(Ball ball, Block block, float moveTime, out CollisionInfo info)
        {
            // block をボールの半径分だけ拡大したAABBを考える
            float expand_left = block.Position.X - ball.Radius;
            float expand_right = block.Position.X + block.Width + ball.Radius; 
            float expand_upper = block.Position.Y - ball.Radius;
            float expand_lower = block.Position.Y + block.Height + ball.Radius;

            info = new CollisionInfo{ Object = block };
            Vector2 normal;
            Vector2 vel = ball.Direction * ball.Speed;
            float dxEntry, dxExit, dyEntry, dyExit;
            
            if(vel.X > 0)
            {
                dxEntry = expand_left - ball.Position.X;
                dxExit = expand_right - ball.Position.X;
                normal.X = -1.0f;
            } else
            {
                dxEntry = expand_right - ball.Position.X;
                dxExit = expand_left - ball.Position.X;
                normal.X = 1.0f;
            }

            if(vel.Y > 0)
            {
                dyEntry = expand_upper - ball.Position.Y;
                dyExit = expand_lower - ball.Position.Y;
                normal.Y = -1.0f;
            } else
            {
                dyEntry = expand_lower - ball.Position.Y;
                dyExit = expand_upper - ball.Position.Y;
                normal.Y = 1.0f;
            }
            
            float txEntry = dxEntry / vel.X*moveTime;
            float tyEntry = dyEntry / vel.Y*moveTime;
            float txExit = dxExit / vel.X*moveTime;
            float tyExit = dyExit / vel.Y*moveTime;

            float maxEntry, minExit;
            // 衝突開始時間はEntryの大きい方
            if(txEntry > tyEntry)
            {
                maxEntry = txEntry;
                normal.Y = 0.0f;
            } else
            {
                maxEntry = tyEntry;
                normal.X = 0.0f;
            }
            // 衝突終了時間はExitの小さい方
            minExit = Math.Min(txExit, tyExit);
            
            info.Normal = normal;
            // 例）ボールが右側に移動していて、ブロックがそれより左にある。
            if(maxEntry < 0 || minExit < 0 || maxEntry > 1.0f || maxEntry > minExit)
            {
                info.EntryTime = float.PositiveInfinity;
                return false;    
            }

            info.EntryTime = maxEntry;
            return true;
        }

        // ballとrectの最近点計算による衝突判定(静的ブロック前提)
        public static bool BallPaddleCollisionCheck(Ball ball, Rectangle rect)
        {
            Vector2 intersect = new(0, 0);  // ボールと長方形の最近点を求める
            Vector2 ballPos = ball.Position ,rectPos = rect.Position;

            // Paddleは動的ブロックなので次フレーム位置を使う
            if(rect is Paddle pd)
            {
                rectPos = pd.NextPosition;
                ballPos = ball.NextFramePosition;
                // TODO: NextPosition使わなくても動くが、互いを向かわせる方向に衝突させると、
                    // Currentの方だと2回衝突する。 NextPositionだと1回しか衝突しない。
                    // CurrentだとSEを鳴らす時に問題になるかも。それ以外は問題なく動きそう。
                // rectPos = pd.Position;
                // ballPos = ball.Position;
            }

            // 最近点のX座標   
            // ball.x が rect より左にある時
            if (ballPos.X < rectPos.X)
            {
                intersect.X = rectPos.X; // 最近点のX座標は rect の左辺
            }
            // ball.x が rect より右にある時
            else if (ballPos.X > rectPos.X + rect.Width)
            {
                intersect.X = rectPos.X + rect.Width;   // 最近点のX座標は rect の右辺
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
            else if (ballPos.Y > rectPos.Y + rect.Height)
            {
                intersect.Y = rectPos.Y + rect.Height;    // 最近点のY座標は rect の底辺
            }
            // ball.y が rect より内側にある時
            else
            {
                intersect.Y = ballPos.Y;
            }

            // 求めた最近点とボールの中心座標で距離を計算して、それが半径より小さければ衝突
             double distanseSqr = Math.Pow(ballPos.X - intersect.X, 2) +
                                    Math.Pow(ballPos.Y - intersect.Y, 2);
            

            if (distanseSqr < Math.Pow(ball.Radius, 2))
            {
                if(rect is Paddle)
                {
                    return true; // Paddleとの衝突は押し戻ししない
                }

                // 最近点からボール中心に向かうベクトル
                Vector2 dist = ballPos - intersect;
                float penetrationDepth = ball.Radius - dist.Length();
                // 押し戻しベクトル
                Vector2 fixUpVector = Vector2.Normalize(dist) * penetrationDepth;
                // ボールの位置を押し戻す
                ball.SetNextPosition(ballPos + fixUpVector);
                ball.ApplyNextPosition();

                return true;
            }
            else
            {
                return false;
            }
        } 
        private static void FixUpOverlap(Ball b, Rectangle r)
        {
            float overlap_right = (b.Position.X+b.Radius) - (r.Position.X);
            float overlap_down = (b.Position.Y+b.Radius) - (r.Position.Y);
            float overlap_left = (r.Position.X+r.Width) - (b.Position.X-b.Radius);
            float overlap_up = (r.Position.Y+r.Height) - (b.Position.Y-b.Radius);

            float overlap_min = Math.Min(Math.Min(overlap_right,overlap_down) ,Math.Min(overlap_left,overlap_up));
            if(overlap_min < 0)
            {
                return; // 重なっていない
            }

            if(overlap_min == overlap_right)
            {
                b.SetNextPosition( new Vector2(r.Position.X - b.Radius, b.Position.Y));
            } else if(overlap_min == overlap_down)
            {
                b.SetNextPosition( new Vector2(b.Position.X, r.Position.Y - b.Radius));
            } else if(overlap_min == overlap_left)
            {
                b.SetNextPosition( new Vector2(r.Position.X + r.Width + b.Radius, b.Position.Y));
            } else if(overlap_min == overlap_up)
            {
                b.SetNextPosition( new Vector2(b.Position.X, r.Position.Y + r.Height + b.Radius));
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
            var wallParams = new List<(Vector2 position, int width, int height, BlockType type)>
            {
                (new Vector2(-wallThickness, -wallThickness), Window.Width + wallThickness * 2, wallThickness, BlockType.Wall),
                (new Vector2(-wallThickness, 0), wallThickness, Window.Height, BlockType.Wall),
                (new Vector2(Window.Width, 0), wallThickness, Window.Height, BlockType.Wall),
                (new Vector2(-wallThickness, Window.Height), Window.Width + wallThickness * 2, wallThickness, BlockType.Wall),
                // (new Vector2(Window.Width/2, _ball.Position.Y-_ball.Radius-wallThickness-5),  wallThickness, wallThickness, BlockType.Destructible)
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
            
            // 物理演算・衝突判定
            _collisionManager.BallMove();
            
            // 座標適用
            // _ball.ApplyNextPosition();
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
                // スペースキーを押している間だけ1フレーム分進める。
                // ポーズ中に 'N' キーで1フレームだけ進めたい場合も有効。
                BeginDrawing();
                if (IsKeyDown(KeyboardKey.Space) || IsKeyPressed(KeyboardKey.N))
                {
                    gameManager.Update();
                    EndDrawing();
                } else
                {
                    gameManager.Update();
                    ClearBackground(Color.Black);
                    EndDrawing();
                }
                
                 
                

                // 描画処理（常に行う）
                
                // 物理処理
                // gameManager.Update();
                // // 応急処置で壁の判定はcollisionMnager .Update()の後に呼ぶようにする。
                // // ResolveWallCollision内で_nextFramePositionを書き換えてしまうため。

                // // ball.ApplyNextPosition();

                // // 描画処理
                // BeginDrawing();
                // ClearBackground(Color.Black);

                // // DrawText("Hello!", 100, 100, 20, Color.White);
                // // ball.Draw();
                // // paddle.Draw();

                // EndDrawing();

            }

            // 3. 終了処理
            CloseWindow();
        }
    }
}