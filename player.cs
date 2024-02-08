using Godot;
using System;
using System.Collections.Generic;

struct Force
{
    public Vector2 force;
    public float deltaTime;

    public Force(Vector2 force, float deltaTime)
    {
        this.force = force;
        this.deltaTime = deltaTime;
    }
};

public partial class player : CharacterBody2D
{
    public const float Speed = 30.0f;
    public const float JumpVelocity = -400.0f;

    private const float mass = 5.0f;
    private const float AIR_FRICTION = 0.0f;
    private const float AERO_DYNAMICITY = 0.5f;

    private int m_NumberOfTicksBeforeGrounded = 0;
    private int m_NumberOfTicksAfterGrounded = 0;

    private bool m_PressedSpace = false;
    private float m_TimePressedSpace = 0;

    private float m_SurfaceFriction = 0.0f;

    private List<Force> m_Forces = new List<Force>();

    private AnimatedSprite2D _animatedSprite;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _animatedSprite.Play("Good Idle");

        Velocity = new Vector2(200, 0);

        MotionMode = MotionModeEnum.Floating;
    }

    public override void _PhysicsProcess(double delta)
    {

        // Handle Jump.
        if (Input.IsActionPressed("ui_select"))
        {

            _animatedSprite.Play("Jump");

            // windows of time to press space
            m_NumberOfTicksBeforeGrounded = 15;

            m_PressedSpace = true;
            m_TimePressedSpace += (float)delta;
        }

        // spacebar released and player is grounded
        if (m_PressedSpace && !Input.IsActionPressed("ui_select") && (MotionMode.Equals(MotionModeEnum.Grounded) || m_NumberOfTicksAfterGrounded > 0))
        {

            float scalar = 1.0f, scalar2 = 1.0f;

            if (m_TimePressedSpace > 1)
            {
                m_TimePressedSpace = 1;
            }

            scalar = m_TimePressedSpace;

            int deltaTime = 5;

            float vertical_force = -200 * gravity * scalar;
            float horizontal_force = -100 * gravity * scalar * scalar2;

            Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

            switch (direction.X)
            {
                case 1:
                    horizontal_force = -horizontal_force;
                    break;
                case -1:
                    break;
                default:
                    horizontal_force = 0;
                    break;
            }
            m_Forces.Add(new Force(new Vector2(horizontal_force, vertical_force), 0.02f));

            MotionMode = MotionModeEnum.Floating;

            m_TimePressedSpace = 0.0f;
            m_PressedSpace = false;
        }

        if (!Input.IsActionPressed("ui_select"))
        {

            if (m_NumberOfTicksBeforeGrounded <= 0)
            {
                m_PressedSpace = false;
                m_TimePressedSpace = 0.0f;
            }
        }

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        //Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        //if (direction != Vector2.Zero)
        //{
        //    //velocity.X = direction.X * Speed;
        //    //m_Forces.Add(new Force(new Vector2(direction.X * Speed, 0), 0.5f));
        //}
        //else
        //{
        //    //velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        //}

        //Velocity = velocity;
        //MoveAndSlide();
        // end

        // Add the gravity.
        // apply gravity
        //Velocity = Velocity + new Vector2(0, gravity * (float)delta);
        if (MotionMode.Equals(MotionModeEnum.Floating))
        {
            //// apply gravity
            //Velocity = Velocity + new Vector2(0, gravity * (float)delta);
            _animatedSprite.Play("Fall");
        }
        else
        {
            _animatedSprite.Play("Good Idle");
        }

        //Calculate air friction
        Vector2 air_friction = Velocity * AIR_FRICTION * AERO_DYNAMICITY;

        // apply forces to velocity
        this.ApplyForcesToVelocity(delta);

        // apply air friction
        Velocity = Velocity - (air_friction / mass) * (float)delta;

        // apply surface friction
        this.ApplySurfaceFriction(delta);

        //MoveAndSlide();

        var collisionInfo = MoveAndCollide(Velocity * (float)delta);

        if (collisionInfo != null)
        {
            var normal = collisionInfo.GetNormal();
            var y = collisionInfo.GetAngle();
            var yy = collisionInfo.GetAngle(new Vector2(1, 0));

            if (y > 0.1 || y < -0.1)
            {
                int ss = 0;
            }

            if (normal.Y < 0)
            {
                MotionMode = MotionModeEnum.Grounded;
            }
            Vector2 velocity = Velocity;
            if (normal.X != 0)
            {
                velocity.X = System.Math.Abs(velocity.X) * normal.X * 0.2f;
            }
            if (normal.Y != 0)
            {
                velocity.Y = System.Math.Abs(velocity.Y) * normal.Y * 0.2f;
            }
            Velocity = velocity;
            //Velocity = new Vector2(System.Math.Abs(Velocity.X) * normal.X * 0.2f, System.Math.Abs(Velocity.Y) * normal.Y * 0.2f);
            //Velocity = Velocity.Bounce(collisionInfo.GetNormal());
        }



    }

    private void ApplyForcesToVelocity(double delta)
    {
        foreach (var force in m_Forces)
        {

            double deltaTimeUsed = force.deltaTime;
            if (force.deltaTime > delta)
            {
                deltaTimeUsed = delta;
            }

            Velocity = Velocity + (force.force / mass) * (float)deltaTimeUsed;
        }

        // substract deltaTime from all forces
        //m_Forces.ForEach(f => f.deltaTime -= (float)delta);
        for (int i = 0; i < m_Forces.Count; i++)
        {
            Force force = m_Forces[i];
            force.deltaTime -= (float)delta;
            m_Forces[i] = force;
        }


        // remove forces that have been used up
        m_Forces.RemoveAll(f => f.deltaTime <= 0);
    }

    private void ApplySurfaceFriction(double delta)
    {
        if (MotionMode.Equals(MotionModeEnum.Grounded))
        {
            Vector2 surface_friction = new Vector2(m_SurfaceFriction * gravity, 0);

            // apply surface friction
            if (Velocity.X > 0)
            {
                Velocity = new Vector2((float)(Velocity.X - surface_friction.X / mass * delta), Velocity.Y);
                if (Velocity.X < 0)
                {
                    Velocity = new Vector2(0, Velocity.Y);
                }
            }
            else
            {
                Velocity = new Vector2((float)(Velocity.X + surface_friction.X / mass * delta), Velocity.Y);
                if (Velocity.X > 0)
                {
                    Velocity = new Vector2(0, Velocity.Y);
                }
            }
        }
    }
}
