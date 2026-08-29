using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class Camera
{
    public Vector3 Position;
    public Vector3 Front = new Vector3(0.0f, 0.0f, -1.0f);
    public Vector3 Up = new Vector3(0.0f, 1.0f, 0.0f);
    public Matrix ViewMatrix {  get; private set; }
    public Matrix ProjectionMatrix { get; private set; }
    private float _yaw = -90.0f; // Set to negative so that we by default point in the negative z direction
    private float _pitch = 0f;

    private float _lastMouseX = 0.0f;
    private float _lastMouseY = 0.0f;
    private bool _resetMouse = false;
    

    public Camera(Vector3 startPosition, float aspectRatio)
    {
        Position = startPosition;
        
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float speed = 10f * dt; 

        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();
    
        if (keyboardState.IsKeyDown(Keys.W)) Position += speed * Front;
        if (keyboardState.IsKeyDown(Keys.S)) Position -= speed * Front;
        if (keyboardState.IsKeyDown(Keys.A)) Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
        if(keyboardState.IsKeyDown(Keys.D)) Position +=  Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;

        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            if (_resetMouse)
            {
                // Mouse was reset.
                _resetMouse = false;
                _lastMouseX = mouseState.X;
                _lastMouseY = mouseState.Y;
            }
            float xOffset = mouseState.X - _lastMouseX;
            float yOffset = _lastMouseY -  mouseState.Y;
            _lastMouseX = mouseState.X;
            _lastMouseY = mouseState.Y;
            const float sensitivity = 0.35f;
            xOffset *= sensitivity;
            yOffset *= sensitivity;
            if (xOffset != 0 || yOffset != 0)
            {
                _yaw += xOffset;
                _pitch += yOffset;
                if (_pitch > 89.0f)
                {
                    _pitch = 89.0f;
                }

                if (_pitch < -89.0f)
                {
                    _pitch = -89.0f;
                }
                Front = Vector3.Normalize(GetDirection());
            }

        }

        if (mouseState.LeftButton == ButtonState.Released)
        {
            // Reset values for offsets. 
            _resetMouse = true;
        }


        
        /*
        else
        {
            if(state.IsKeyDown(Keys.W)) Position += Vector3.Forward * speed;
            if(state.IsKeyDown(Keys.S)) Position += Vector3.Backward * speed;
            if(state.IsKeyDown(Keys.A)) Position += Vector3.Left * speed;
            if(state.IsKeyDown(Keys.D)) Position += Vector3.Right * speed;

        }
        */
        
        ViewMatrix = Matrix.CreateLookAt(Position, Position + Front, Up);
    }

    public Vector3 GetDirection()
    {
        return new Vector3((float) (Math.Cos(MathHelper.ToRadians(_yaw)) * Math.Cos(MathHelper.ToRadians(_pitch))), MathHelper.ToRadians(_pitch),  (float) (Math.Sin(MathHelper.ToRadians(_yaw)) * Math.Cos(MathHelper.ToRadians(_pitch))));
    }
}