using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpGLTF.Schema2;


namespace Shaxer;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private RenderTarget2D _pixelRenderTarget;
    
    private const int RenderWidth = 480;
    private const int RenderHeight = 270;

    private List<(VertexBuffer Vertices, IndexBuffer Indices, int TriangleCount)> _meshes = new();
    private Camera _camera;
    private BasicEffect _effect;
    private Effect _toonEffect; // Custom Shader
    


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialize Camera w Device's Aspect Ratio
        // float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixelRenderTarget = new RenderTarget2D(GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color,
            DepthFormat.Depth24);
        
        float internalAspectRatio = (float) RenderWidth / RenderHeight;
        _camera = new Camera(new Vector3(0, 10, 20), internalAspectRatio);
        /*
        _effect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true, LightingEnabled = true, PreferPerPixelLighting = false };
        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1f, -1.5f, -1f));
        _effect.DirectionalLight0.DiffuseColor = new Vector3(0.9f, 0.85f, 0.8f);

        _effect.AmbientLightColor = new Vector3(0.25f, 0.25f, 0.35f);
        */
        base.Initialize();
    }

    protected override void LoadContent()
    {
        
        string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "ToonShader.mgfx");
        
        if (!File.Exists(shaderPath))
        {
            throw new FileNotFoundException($"Compiled shader not found at '{shaderPath}'. Push ToonShader.fx to GitHub to auto-generate it!");
        }

        byte[] shaderBytes = File.ReadAllBytes(shaderPath);
        _toonEffect = new Effect(GraphicsDevice, shaderBytes);
        
        Console.WriteLine("=== TOON SHADER PARAMETER REFLECTION ===");
        foreach (var param in _toonEffect.Parameters)
        {
            Console.WriteLine($"Name: '{param.Name}'");
            Console.WriteLine($"  Class: {param.ParameterClass}");
            Console.WriteLine($"  Type:  {param.ParameterType}");
            Console.WriteLine($"  Rows:  {param.RowCount}");
            Console.WriteLine($"  Cols:  {param.ColumnCount}");
            Console.WriteLine($"  Elements: {param.Elements.Count}");
            Console.WriteLine("---------------------------------------");
        }
        // Load GLTF directly via pure C#
        
        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "pixel_scene.glb");
        var modelRoot = ModelRoot.Load(fullPath);

        // Get the primary scene in the GLTF file
        var scene = modelRoot.DefaultScene ?? modelRoot.LogicalScenes[0];

        // Traverse all drawable visual instances in the scene
        foreach (var instance in scene.VisualChildren)
        {
            var mesh = instance.Mesh;
            if (mesh == null) continue;

            // Get the final transform matrix for this specific object in the scene
            Matrix worldMatrix = ConvertMatrix(instance.WorldMatrix);

            foreach (var primitive in mesh.Primitives)
            {
                var posAccess = primitive.GetVertexAccessor("POSITION").AsVector3Array();
                var normAccess = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                if (posAccess == null)
                {
                    continue;
                }

                var baseColor = primitive.Material?.FindChannel("BaseColor").Value;
                
                Color meshColor = Color.Azure; // Fallback default

                // Grabs Color from the Material
                /*
                if (baseColor.HasValue)
                {
                    var c = baseColor.Value;
                    meshColor = c.Color;
                }
                */
                
                
                var vertices = new VertexPositionColorNormal[posAccess.Count];

                for (int i = 0; i < posAccess.Count; i++)
                {
                    var p = posAccess[i];
                
                    // Transform the local vertex position by the node's World Matrix
                    Vector3 localPos = new Vector3(p.X, p.Y, p.Z);
                    Vector3 worldPos = Vector3.Transform(localPos, worldMatrix);
                    
                    Vector3 normal = Vector3.Up;
                    if (normAccess != null && i < normAccess.Count)
                    {
                        Vector3 localNorm = new Vector3(normAccess[i].X, normAccess[i].Y, normAccess[i].Z);
                        normal = Vector3.TransformNormal(localNorm, worldMatrix);
                        normal.Normalize();
                    }
                    if (worldPos.Y > 0.5f)
                    {
                        meshColor = Color.Silver;
                    }
                    else
                    {
                        meshColor = Color.ForestGreen;
                    }
                    vertices[i] = new VertexPositionColorNormal(worldPos, meshColor, normal);
                }

                var vBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorNormal), vertices.Length, BufferUsage.WriteOnly);
                vBuffer.SetData(vertices);

                var indices = primitive.GetIndices();
                var iBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                iBuffer.SetData(indices.ToArray());

                _meshes.Add((vBuffer, iBuffer, indices.Count / 3));
            }
        }
    }

// Helper to convert System.Numerics.Matrix4x4 (SharpGLTF) to MonoGame Matrix
    private Matrix ConvertMatrix(System.Numerics.Matrix4x4 m)
    {
        return new Matrix(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        
        _camera.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_pixelRenderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);
    
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
    
        // Calculate ViewProjection (Vertices are already in World space)
        Matrix viewProj = _camera.ViewMatrix * _camera.ProjectionMatrix;

        // Set updated parameters
        _toonEffect.Parameters["ViewProjection"]?.SetValue(viewProj);

        Vector3 lightDir = Vector3.Normalize(new Vector3(-1f, -1.5f, -1f));
        _toonEffect.Parameters["LightDirection"]?.SetValue(new Vector4(lightDir, 0f));

        _toonEffect.Parameters["LightColor"]?.SetValue(new Vector4(1.0f, 0.95f, 0.85f, 1f));
        _toonEffect.Parameters["AmbientColor"]?.SetValue(new Vector4(0.25f, 0.25f, 0.35f, 1f));
        _toonEffect.Parameters["ShaderParams"]?.SetValue(new Vector4(3.0f, 0f, 0f, 0f));

        foreach (var pass in _toonEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            foreach (var mesh in _meshes)
            {
                GraphicsDevice.SetVertexBuffer(mesh.Vertices);
                GraphicsDevice.Indices = mesh.Indices;
                GraphicsDevice.DrawIndexedPrimitives(
                    Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 
                    0, 
                    0, 
                    mesh.TriangleCount
                );
            }
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
    
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null);
        _spriteBatch.Draw(_pixelRenderTarget, GraphicsDevice.Viewport.Bounds, Color.White);
        _spriteBatch.End();
    
        base.Draw(gameTime);
    }
}
