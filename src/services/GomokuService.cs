using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MyBot.Services


{
    
    public enum StoneType
    {
        Empty, // 空格
        Black, // 黑棋
        White  // 白棋
    }

    public class GomokuService
    {
        private const int BoardSize = 15; // 棋盘大小
        private readonly StoneType[,] _board = new StoneType[BoardSize, BoardSize]; // 使用 StoneType 枚举
        private bool _isPlayerTurn = true; // 玩家先手

        public GomokuService()
        {
            InitializeBoard();
        }
        private int EvaluateBoard()
        {
            int score = 0;

            // 遍历棋盘，评估每个位置的连珠情况
            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    if (_board[x, y] == StoneType.Empty) continue;

                    // 检查四个方向：水平、垂直、主对角线、副对角线
                    int[] dx = { 1, 0, 1, 1 };
                    int[] dy = { 0, 1, 1, -1 };

                    foreach (var direction in Enumerable.Range(0, 4))
                    {
                        score += EvaluateDirection(_board, x, y, dx[direction], dy[direction]);
                    }
                }
            }

            return score;
        }
        public (int x, int y) GetBestMove(int depth, bool isMaximizingPlayer)
        {
            int bestScore = isMaximizingPlayer ? int.MinValue : int.MaxValue;
            (int x, int y) bestMove = (-1, -1);

            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    if (_board[x, y] != StoneType.Empty) continue;

                    // 尝试放置棋子
                    _board[x, y] = isMaximizingPlayer ? StoneType.Black : StoneType.White;

                    // 递归调用 Minimax
                    int score = Minimax(depth - 1, !isMaximizingPlayer, int.MinValue, int.MaxValue);

                    // 撤销棋子
                    _board[x, y] = StoneType.Empty;

                    // 更新最佳分数和最佳走法
                    if ((isMaximizingPlayer && score > bestScore) ||
                        (!isMaximizingPlayer && score < bestScore))
                    {
                        bestScore = score;
                        bestMove = (x, y);
                    }
                }
            }

            return bestMove;
        }

        private int Minimax(int depth, bool isMaximizingPlayer, int alpha, int beta)
        {
            if (depth == 0)
            {
                return EvaluateBoard(); // 使用启发式函数评估局势
            }

            if (isMaximizingPlayer)
            {
                int maxScore = int.MinValue;
                for (int x = 0; x < BoardSize; x++)
                {
                    for (int y = 0; y < BoardSize; y++)
                    {
                        if (_board[x, y] != StoneType.Empty) continue;

                        // 尝试放置棋子
                        _board[x, y] = StoneType.Black;

                        // 递归调用
                        int score = Minimax(depth - 1, false, alpha, beta);

                        // 撤销棋子
                        _board[x, y] = StoneType.Empty;

                        // 更新最大值
                        maxScore = Math.Max(maxScore, score);
                        alpha = Math.Max(alpha, score);

                        // Alpha-Beta 剪枝
                        if (beta <= alpha) break;
                    }
                }
                return maxScore;
            }
            else
            {
                int minScore = int.MaxValue;
                for (int x = 0; x < BoardSize; x++)
                {
                    for (int y = 0; y < BoardSize; y++)
                    {
                        if (_board[x, y] != StoneType.Empty) continue;

                        // 尝试放置棋子
                        _board[x, y] = StoneType.White;

                        // 递归调用
                        int score = Minimax(depth - 1, true, alpha, beta);

                        // 撤销棋子
                        _board[x, y] = StoneType.Empty;

                        // 更新最小值
                        minScore = Math.Min(minScore, score);
                        beta = Math.Min(beta, score);

                        // Alpha-Beta 剪枝
                        if (beta <= alpha) break;
                    }
                }
                return minScore;
            }
        }

        private int EvaluateDirection(StoneType[,] board, int x, int y, int dx, int dy)
        {
            int myScore = 0, opponentScore = 0;

            // 检查当前方向上的连续棋子
            for (int step = 1; step <= 5; step++)
            {
                int nx = x + step * dx;
                int ny = y + step * dy;

                if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize) break;

                if (board[nx, ny] == StoneType.Black)
                {
                    myScore++;
                }
                else if (board[nx, ny] == StoneType.White)
                {
                    opponentScore++;
                }
                else
                {
                    break; // 遇到空格停止
                }
            }

            // 根据连续棋子数量评分
            int[] scoreTable = { 0, 1, 10, 100, 1000, 10000 }; // 两连、三连、四连等的价值
            return scoreTable[myScore] - scoreTable[opponentScore];
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    _board[i, j] = StoneType.Empty; // 初始化为空格
                }
            }
        }

        public string DrawBoard()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("   " + string.Join(" ", Enumerable.Range(1, BoardSize).Select(n => n.ToString().PadLeft(2))));
            for (int i = 0; i < BoardSize; i++)
            {
                sb.Append((char)('a' + i) + " ");
                for (int j = 0; j < BoardSize; j++)
                {
                    char symbol = _board[i, j] switch
                    {
                        StoneType.Empty => '.', // 空格
                        StoneType.Black => 'X', // 黑棋
                        StoneType.White => 'O', // 白棋
                        _ => throw new InvalidOperationException("Invalid stone type.")
                    };
                    sb.Append(symbol + "  ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public bool PlaceStone(char rowLetter, int colNumber)
        {
            int x = rowLetter - 'a'; // 字母转行索引
            int y = colNumber - 1;   // 数字转列索引

            if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize || _board[x, y] != StoneType.Empty)
            {
                return false; // 非法操作
            }

            _board[x, y] = _isPlayerTurn ? StoneType.Black : StoneType.White; // 放置棋子
            _isPlayerTurn = !_isPlayerTurn; // 切换玩家
            return true;
        }

        public bool CheckWin(int x, int y)
        {
            StoneType stone = _board[x, y];
            if (stone == StoneType.Empty) return false;

            int[] dx = { 1, 0, 1, 1 }; // 四个方向：水平、垂直、主对角线、副对角线
            int[] dy = { 0, 1, 1, -1 };

            for (int i = 0; i < 4; i++)
            {
                int count = 1;
                for (int step = 1; step < 5; step++)
                {
                    int nx = x + step * dx[i];
                    int ny = y + step * dy[i];
                    if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize && _board[nx, ny] == stone)
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                for (int step = 1; step < 5; step++)
                {
                    int nx = x - step * dx[i];
                    int ny = y - step * dy[i];
                    if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize && _board[nx, ny] == stone)
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (count >= 5) return true;
            }

            return false;
        }

        public Image GenerateBoardImage(int? highlightRow = null, int? highlightCol = null)
        {
            int boardSize = 15; // 棋盘大小为 15x15
            int cellSize = 40;  // 每个格子的大小
            int imageSize = boardSize * cellSize + 1;

            Bitmap image = new Bitmap(imageSize, imageSize);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.LightSlateGray); // 设置背景为灰色
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // 抗锯齿

                // 绘制棋盘网格
                for (int i = 0; i <= boardSize; i++)
                {
                    int position = i * cellSize;
                    g.DrawLine(Pens.Black, position, 0, position, imageSize - 1); // 竖线
                    g.DrawLine(Pens.Black, 0, position, imageSize - 1, position); // 横线
                }

                // 在每个格子上绘制坐标
                using (Font font = new Font("Arial", 12)) // 使用较小字体
                using (Brush textBrush = new SolidBrush(Color.DarkOrange)) // 文字颜色为灰色
                {
                    for (int col = 0; col < boardSize; col++)
                    {
                        for (int row = 0; row < boardSize; row++)
                        {
                            // 计算当前格子的中心位置
                            int centerX = col * cellSize + cellSize / 2;
                            int centerY = row * cellSize + cellSize / 2;

                            // 生成坐标文本，如 "a1", "a2"
                            string coordinate = $"{(char)('a' + col)}{row + 1}";

                            // 如果是高亮格子，则先绘制浅色背景
                            if (highlightRow.HasValue && highlightCol.HasValue &&
                                row == highlightRow.Value && col == highlightCol.Value)
                            {
                                using (Brush highlightBrush = new SolidBrush(Color.FromArgb(200, 200, 255))) // 浅蓝色
                                {
                                    g.FillRectangle(highlightBrush, col * cellSize, row * cellSize, cellSize, cellSize);
                                }
                            }

                            // 绘制坐标文本
                            SizeF textSize = g.MeasureString(coordinate, font);
                            float textX = centerX - textSize.Width / 2;
                            float textY = centerY - textSize.Height / 2;
                            g.DrawString(coordinate, font, textBrush, textX, textY);
                        }
                    }
                }

                // 绘制已有棋子
                for (int x = 0; x < boardSize; x++)
                {
                    for (int y = 0; y < boardSize; y++)
                    {
                        if (_board[x, y] == StoneType.Black)
                        {
                            DrawStone(g, x, y, Color.Black);
                        }
                        else if (_board[x, y] == StoneType.White)
                        {
                            DrawStone(g, x, y, Color.White);
                        }
                    }
                }
            }
            return image;
        }

        private void DrawStone(Graphics g, int x, int y, Color color)
        {
            int cellSize = 40;
            int stoneRadius = cellSize / 2 - 2;

            using (Brush brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, x * cellSize + 2, y * cellSize + 2, stoneRadius * 2, stoneRadius * 2);
            }
        }
    }
}