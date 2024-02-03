# Toolkid.Bounds

This repository provides a collection of extension methods for Bounds, including Collider, Renderer, Mesh, and more.

## Collider Maker

The **Collider Maker** allows you to add a consolidated collider to multiple models.

### Usage

1. Add `Collider Maker` to the object where you want to generate colliders.
2. Drag the object containing all models to the `Meshes Base`.
3. Click on the desired collider style (`Box` or `Sphere`).

![Inspector](https://github.com/hhs456/Toolkid.BoundsUtility/blob/main/Description/inspector.jpg)

### Results

1. **Box**

![Multi Cube](https://github.com/hhs456/Toolkid.BoundsUtility/blob/main/Description/multiCube.jpg)

2. **Sphere (Inside)**

![Inside Ball](https://github.com/hhs456/Toolkid.BoundsUtility/blob/main/Description/insideBall.jpg)

3. **Sphere (Outside)**

![Outside Ball](https://github.com/hhs456/Toolkid.BoundsUtility/blob/main/Description/outsideBall.jpg)

### Notes

Currently, effective only for objects with a `rotation` of (0, 0, 0). Please use with caution!
