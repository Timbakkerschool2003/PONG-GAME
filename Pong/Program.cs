using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public interface ICommand
{
    void Execute(PongGame game);
}

public class Command : ICommand
{
    public Action ExecuteAction { get; set; }

    public void Execute(PongGame game)
    {
        ExecuteAction?.Invoke();
    }
}

public interface IInputHandler
{
    void AddCommand(ConsoleKey key, ICommand command);
    void HandleInput(PongGame game);
}

public class InputHandler : IInputHandler
{
    private readonly Dictionary<ConsoleKey, ICommand> _commands = new Dictionary<ConsoleKey, ICommand>();

    public void AddCommand(ConsoleKey key, ICommand command)
    {
        _commands[key] = command;
    }

    public void HandleInput(PongGame game)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (_commands.ContainsKey(key))
            {
                _commands[key].Execute(game);
            }
        }
    }
}

public class LoggingInputHandlerDecorator : IInputHandler
{
    private readonly IInputHandler _inputHandler;

    public LoggingInputHandlerDecorator(IInputHandler inputHandler)
    {
        _inputHandler = inputHandler;
    }

    public void AddCommand(ConsoleKey key, ICommand command)
    {
        _inputHandler.AddCommand(key, command);
    }

    public void HandleInput(PongGame game)
    {
        _inputHandler.HandleInput(game);
    }
}

public interface IMonitor
{
    void DisplayStatus(PongGame game);
}

public class ScoreMonitor : IMonitor
{
    public void DisplayStatus(PongGame game)
    {
        string scoreText = $"Player 1: {game.Player1Score}, Player 2: {game.Player2Score}";
        int leftPadding = (Console.WindowWidth - scoreText.Length) / 2 - 20;
        Console.SetCursorPosition(leftPadding, Console.CursorTop);
        Console.WriteLine(scoreText);
    }
}




public class PaddlePositionMonitor : IMonitor
{
    public void DisplayStatus(PongGame game)
    {
        Console.WriteLine($"Paddle Positions - Player 1: {game.Paddle1Position}, Player 2: {game.Paddle2Position}");
    }
}

public class BallPositionMonitor : IMonitor
{
    public void DisplayStatus(PongGame game)
    {
        Console.WriteLine($"Ball Position - X: {game.BallX}, Y: {game.BallY}");
    }
}

public class PongGame
{
    private int _paddle1Position, _paddle2Position;
    private int _ballX, _ballY;
    private int _ballVelocityX, _ballVelocityY;
    private const int _playfieldWidth = 80;
    private const int _playfieldHeight = 24;
    private const int _paddleHeight = 4;
    private int _player1Score, _player2Score;
    private bool _gameRunning = true;
    private DateTime _lastBallMoveTime;
    private bool _paused = false;
    private readonly IInputHandler _inputHandler;
    private readonly BlockingCollection<ICommand> _commandQueue = new BlockingCollection<ICommand>();

    public int Paddle1Position { get { return _paddle1Position; } }
    public int Paddle2Position { get { return _paddle2Position; } }
    public int BallX { get { return _ballX; } }
    public int BallY { get { return _ballY; } }
    public int Player1Score { get { return _player1Score; } }
    public int Player2Score { get { return _player2Score; } }

    public PongGame(IInputHandler inputHandler)
    {
        Console.CursorVisible = false;
        _inputHandler = inputHandler;
        _inputHandler.AddCommand(ConsoleKey.W, new MovePaddleUpCommand(1));
        _inputHandler.AddCommand(ConsoleKey.S, new MovePaddleDownCommand(1));
        _inputHandler.AddCommand(ConsoleKey.UpArrow, new MovePaddleUpCommand(2));
        _inputHandler.AddCommand(ConsoleKey.DownArrow, new MovePaddleDownCommand(2));
        _inputHandler.AddCommand(ConsoleKey.Escape, new PauseCommand());
        Task.Factory.StartNew(Worker);
        ResetGame();
    }

    public void ResetGame()
    {
        _paddle1Position = _paddle2Position = _playfieldHeight / 2 - _paddleHeight / 2;
        _player1Score = 0;
        _player2Score = 0;
        ResetBall();
        Console.Clear();
    }

    private void ResetBall()
    {
        _ballX = _playfieldWidth / 2;
        _ballY = new Random().Next(1, _playfieldHeight - 1);
        _ballVelocityX = new Random().Next(0, 2) * 2 - 1;
        _ballVelocityY = new Random().Next(0, 2) * 2 - 1;
        _lastBallMoveTime = DateTime.Now;
    }

    public void Run(IMonitor monitor)
    {
        while (_gameRunning)
        {
            if (!_paused)
            {
                if ((DateTime.Now - _lastBallMoveTime).TotalMilliseconds > 100)
                {
                    MoveBall();
                    CheckCollision();
                    _lastBallMoveTime = DateTime.Now;
                }
            }

            _inputHandler.HandleInput(this);
            Draw();
            monitor.DisplayStatus(this);
            Thread.Sleep(20);
        }
    }

    private void Worker()
    {
        while (true)
        {
            var command = _commandQueue.Take();
            command.Execute(this);
        }
    }

    private void EnqueueCommand(ICommand command)
    {
        _commandQueue.Add(command);
    }

    public void EnqueueMovePaddleUp(int player)
    {
        EnqueueCommand(new MovePaddleUpCommand(player));
    }

    public void EnqueueMovePaddleDown(int player)
    {
        EnqueueCommand(new MovePaddleDownCommand(player));
    }

    public void EnqueuePause()
    {
        EnqueueCommand(new PauseCommand());
    }

    private void MoveBall()
    {
        _ballX += _ballVelocityX;
        _ballY += _ballVelocityY;
    }

    private void CheckCollision()
    {
        if (_ballY <= 1 || _ballY >= _playfieldHeight - 2)
        {
            _ballVelocityY = -_ballVelocityY;
        }

        if (_ballX == 3 && _ballY >= _paddle1Position && _ballY <= _paddle1Position + _paddleHeight)
        {
            _ballVelocityX = -_ballVelocityX;
        }

        if (_ballX == _playfieldWidth - 4 && _ballY >= _paddle2Position && _ballY <= _paddle2Position + _paddleHeight)
        {
            _ballVelocityX = -_ballVelocityX;
        }

        if (_ballX < 1)
        {
            _player2Score++;
            ResetBall();
        }
        else if (_ballX > _playfieldWidth - 2)
        {
            _player1Score++;
            ResetBall();
        }
    }

    public void MovePaddleUp(int player)
    {
        if (player == 1)
            _paddle1Position = Math.Max(1, _paddle1Position - 1);
        else if (player == 2)
            _paddle2Position = Math.Max(1, _paddle2Position - 1);
    }

    public void MovePaddleDown(int player)
    {
        if (player == 1)
            _paddle1Position = Math.Min(_playfieldHeight - _paddleHeight - 1, _paddle1Position + 1);
        else if (player == 2)
            _paddle2Position = Math.Min(_playfieldHeight - _paddleHeight - 1, _paddle2Position + 1);
    }

    public void Pause()
    {
        _paused = !_paused;
    }

    public void Stop()
    {
        _gameRunning = false;
    }

    private void Draw()
    {
        Console.SetCursorPosition(0, 0);
        StringBuilder frame = new StringBuilder();

        for (int y = 0; y <= _playfieldHeight; y++)
        {
            for (int x = 0; x <= _playfieldWidth; x++)
            {
                if (x == 0 || y == 0 || x == _playfieldWidth || y == _playfieldHeight)
                {
                    frame.Append("*");
                }
                else if (x == 2 && y >= _paddle1Position && y < _paddle1Position + _paddleHeight)
                {
                    frame.Append("|");
                }
                else if (x == _playfieldWidth - 3 && y >= _paddle2Position && y < _paddle2Position + _paddleHeight)
                {
                    frame.Append("|");
                }
                else if (x == _ballX && y == _ballY)
                {
                    frame.Append("■");
                }
                else
                {
                    frame.Append(" ");
                }
            }
            frame.AppendLine();
        }

        Console.Write(frame.ToString());
    }

}

public class PongGameFacade
{
    private readonly PongGame _game;

    public PongGameFacade(PongGame game)
    {
        _game = game;
    }

    public void StartGame()
    {
        _game.ResetGame();
        _game.Run(new ScoreMonitor());
    }

    public void StopGame()
    {
        _game.Stop();
    }
}

public class MovePaddleUpCommand : ICommand
{
    private readonly int _player;

    public MovePaddleUpCommand(int player)
    {
        _player = player;
    }

    public void Execute(PongGame game)
    {
        game.MovePaddleUp(_player);
    }
}

public class MovePaddleDownCommand : ICommand
{
    private readonly int _player;

    public MovePaddleDownCommand(int player)
    {
        _player = player;
    }

    public void Execute(PongGame game)
    {
        game.MovePaddleDown(_player);
    }
}

public class PauseCommand : ICommand
{
    public void Execute(PongGame game)
    {
        game.Pause();
    }
}

public class Program
{
    static void Main()
    {
        var baseInputHandler = new InputHandler();
        var decoratedInputHandler = new LoggingInputHandlerDecorator(baseInputHandler);
        var game = new PongGame(decoratedInputHandler);
        var gameFacade = new PongGameFacade(game);

        gameFacade.StartGame();
    }
}
