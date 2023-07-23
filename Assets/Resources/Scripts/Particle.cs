using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Particle
{
    public Vector2 pos;
    public Vector2 dir;
    public float velocity;
    public float waterContent;
    public float sedimentContent;
    public float carryCapacity;

    public void Initialize(Vector2 pos, float velocity, float waterContent, float sedimentContent, float carryCapacity) {
        this.pos = pos;
        this.velocity = velocity;
        this.waterContent = waterContent;
        this.sedimentContent = sedimentContent;
        this.carryCapacity = carryCapacity;
    }

    // Returns true if the particle is carrying more sediment than it has capacity for
    public bool IsOverloaded() {
        return sedimentContent > carryCapacity;
    }
}