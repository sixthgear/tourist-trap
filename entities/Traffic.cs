using Godot;
// using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

[Tool]
public class Traffic : Node2D
{
    public enum TrafficDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public string[] Colors = {
        "blue", "red", "black", "white"
    };
    
    private TrafficDirection _direction = TrafficDirection.Right;
    private int _numberCarsAhead = 5;
    private int _numberCarsBehind = 5;
    private Array<KinematicBody2D> _cars = new Array<KinematicBody2D>();
    
    private float _speed = 10;
    private float _currentSpeed = 10;
    private bool _stopped = false;

    public Array Lights;

    [Export] private TrafficDirection Direction
    {
        get => _direction;
        set => SetDirection(value);
    }
    public void SetDirection(TrafficDirection dir)
    {
        _direction = dir;
        SpawnCars();
    }
    [Export (PropertyHint.Range, "1,10,")] public int NumberCarsAhead {
        get => _numberCarsAhead;
        set => SetNumberCarsAhead(value);
    }
    [Export (PropertyHint.Range, "1,10,")] public int NumberCarsBehind {
        get => _numberCarsBehind;
        set => SetNumberCarsBehind(value);
    }

    public void SetNumberCarsAhead(int num)
    {
        if (num != _numberCarsAhead)
        {
            _numberCarsAhead = num;
            SpawnCars();
        }
    }

    public void SetNumberCarsBehind(int num)
    {
        if (num != _numberCarsBehind)
        {
            _numberCarsBehind = num;
            SpawnCars();
        }
    }

    public Vector2 GetTrafficVector()
    {
        switch (Direction)
        {
            case TrafficDirection.Left:
                return Vector2.Left;
            case TrafficDirection.Right:
                return Vector2.Right;
            case TrafficDirection.Up:
                return Vector2.Up;
            case TrafficDirection.Down:
                return Vector2.Down;
            default:
                return Vector2.Zero;
        }
    }

    public Vector2 GetRespawnPoint()
    {
        return (GetTrafficVector() * 192) + GetTrafficVector() * _numberCarsBehind * -384;
    }
    
    public Vector2 GetDespawnPoint()
    {
        return (GetTrafficVector() * -192) + GetTrafficVector() * _numberCarsAhead * 384;
    }
    
    
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Lights = GetTree().Root.GetNode<YSort>("Game/Entities/Lights").GetChildren();
        GD.Print(Lights);
        SpawnCars();
        GD.Print("respawnAt: ", GetRespawnPoint());
        GD.Print("despawnAt: ", GetDespawnPoint());
    }

    public void Toggle(bool? value)
    {
        GD.Print("Toggle!");
        if (value.HasValue)
            _stopped = value.Value;
        else
            _stopped = !_stopped;
        foreach (StaticBody2D light in Lights)
        {
            var sprite = light.GetNode<AnimatedSprite>("AnimatedSprite");
            sprite.Frame = 0;
            if (_stopped)
                sprite.Play();
            else
                sprite.Stop();
        }
        
    }

    public void ClearCars()
    {
        // Remove Existing cars
        foreach (Node2D car in _cars)
        {
            // GD.Print("Removing car: ", car);
            car.QueueFree();
        }
        _cars.Clear();
    }

    public void DespawnCar(KinematicBody2D car)
    {
        // GD.Print("Despawning car ", car);
        _cars.Remove(car);
        car.QueueFree();
    }

    public void SpawnCars()
    {
        Vector2 spawnPos = GetDespawnPoint();
        int totalCars = _numberCarsBehind + _numberCarsAhead;
        
        ClearCars();
        
        // Add new cars        
        foreach (int value in Enumerable.Range(1, totalCars))
        {
            SpawnCar(spawnPos);
            spawnPos += GetTrafficVector() * -384;
        }
    }

    public void SpawnCar(Vector2 position = default)
    {
        PackedScene carScene;
        KinematicBody2D car;
        AnimatedSprite carSprite;
        // pick a random color
        string colorSuffix = "_" + Colors[GD.Randi() % Colors.Length];
        
        switch (Direction)
        {
            case TrafficDirection.Left:
                carScene = GD.Load<PackedScene>("entities/CarHorizontal.tscn");
                car = carScene.Instance<KinematicBody2D>();
                carSprite = car.GetNode<AnimatedSprite>("AnimatedSprite");
                carSprite.FlipH = true;
                carSprite.Play("running" + colorSuffix);
                break;
            case TrafficDirection.Right:
                carScene = GD.Load<PackedScene>("entities/CarHorizontal.tscn");
                car = carScene.Instance<KinematicBody2D>();
                carSprite = car.GetNode<AnimatedSprite>("AnimatedSprite");
                carSprite.FlipH = false;
                carSprite.Play("running" + colorSuffix);
                break;
            case TrafficDirection.Up:
                carScene = GD.Load<PackedScene>("entities/CarVertical.tscn");
                car = carScene.Instance<KinematicBody2D>();
                carSprite = car.GetNode<AnimatedSprite>("AnimatedSprite");
                carSprite.Play("running_up" + colorSuffix);
                break;
            case TrafficDirection.Down:
                carScene = GD.Load<PackedScene>("entities/CarVertical.tscn");
                car = carScene.Instance<KinematicBody2D>();
                carSprite = car.GetNode<AnimatedSprite>("AnimatedSprite");
                carSprite.Play("running_down" + colorSuffix);
                break;
            default:
                return;
        }
        
        // GD.Print("Adding car: ", car, position);
        car.Position = position;
        AddChild(car);
        _cars.Insert(0, car);
    }

    public override void _PhysicsProcess(float delta)
    {
        // don't animate in the editor
        if (Engine.EditorHint)
            return; 
        
        Vector2 respawnAt = GetRespawnPoint();
        Vector2 despawnAt = GetDespawnPoint();
        
        // Lookup first and last cars
        var end = _cars.First();
        var front = _cars.Last();

        // spawn new cars in space behind
        if (_direction == TrafficDirection.Left && end.Position.x <= respawnAt.x - 384)
            SpawnCar(respawnAt);
        else if (_direction == TrafficDirection.Right && end.Position.x >= respawnAt.x + 384)
            SpawnCar(respawnAt);
        else if (_direction == TrafficDirection.Up && end.Position.y <= respawnAt.y - 384)
            SpawnCar(respawnAt);
        else if (_direction == TrafficDirection.Down && end.Position.y >= respawnAt.y + 384)
            SpawnCar(respawnAt);
        
        // despawn cars at front of line;
        if (_direction == TrafficDirection.Left && front.Position.x <= despawnAt.x)
            DespawnCar(front);
        else if (_direction == TrafficDirection.Right && front.Position.x >= despawnAt.x)
            DespawnCar(front);
        else if (_direction == TrafficDirection.Up && front.Position.y <= despawnAt.y)
            DespawnCar(front);
        else if (_direction == TrafficDirection.Down && front.Position.y >= despawnAt.y)
            DespawnCar(front);


        if (_stopped)
        {
            if (_currentSpeed > 0)
                _currentSpeed -= 0.05f;
            // if (_currentSpeed < 0.1)
            //     _currentSpeed = 0;
        }
        else
        {
            if (_currentSpeed < _speed)
                _currentSpeed += 0.05f;
            // if (_currentSpeed > _speed * 0.95)
            //     _currentSpeed = _speed;
        }

        foreach (var car in _cars)
        {
            if (_stopped && _direction == TrafficDirection.Left && car.Position.x <= 0)
                car.Position += GetTrafficVector() * _speed;
            else if (_stopped && _direction == TrafficDirection.Right && car.Position.x >= 0)
                car.Position += GetTrafficVector() * _speed;
            else if (_stopped && _direction == TrafficDirection.Up && car.Position.y <= 0)
                car.Position += GetTrafficVector() * _speed;
            else if (_stopped && _direction == TrafficDirection.Down && car.Position.y >= 0)
                car.Position += GetTrafficVector() * _speed;
            else
                car.Position += GetTrafficVector() * _currentSpeed;
            
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
            switch ((KeyList) key.Scancode)
            {
                case KeyList.Z:
                    _stopped = !_stopped;
                    break;
            }
    }
}
