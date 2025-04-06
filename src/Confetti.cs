using Godot;
using System;
using System.Collections.Generic;

public partial class Confetti : Control
{
    private const int HOW_MANY_BLOCKS = 1;

    private List<GpuParticles2D> particles = [];

    Confetti()
    {
        Find.Confetti = this;
    }

    public override void _Ready()
    {
        var particlesBlock = GD.Load<PackedScene>("res://scenes/particles_block.tscn");

        for (var i = 0; i < HOW_MANY_BLOCKS; i++)
        {
            var particleBlock = particlesBlock.Instantiate<Control>();
            GetNode("./HBox").AddChild(particleBlock);

            particles.Add(particleBlock.GetNode<GpuParticles2D>("./Holder/Particles"));
        }
    }

    public void Trigger()
    {
        foreach (var particle in particles)
        {
            particle.Emitting = true;
        }
    }
}
