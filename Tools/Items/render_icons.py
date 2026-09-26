"""Render the six shipped GLBs as individual, transparent inventory icons.

Run with Blender --background --factory-startup --python this_file -- --project PATH.
The source models and their embedded materials are never modified.
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ITEMS = {9: "Seaweed_BAG", 12: "WaterBottle_BAG", 13: "SilverFish_BAG",
         16: "EnergyBar_BAG", 18: "TealGem_BAG", 20: "Item_Cloth_BAG"}


def point_at(obj, target):
    """Aim the camera or area light at the model's center."""
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def add_light(name, location, power, size, color):
    """Create a soft studio light with repeatable exposure and direction."""
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size, data.color = power, 'DISK', size, color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, (0, 0, 0))


def render_item(project, item_id, model_name, samples):
    """Import one original model and render a centered orthographic RGBA icon."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    bpy.ops.import_scene.gltf(filepath=str(project / 'Assets/Resources/FBX/Items' / (model_name + '.glb')))
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
    low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center, factor = (low + high) / 2, 2.0 / max(high - low)
    # Preserve child transforms while normalizing only top-level imported objects.
    root = bpy.data.objects.new('IconPivot', None)
    scene.collection.objects.link(root)
    for obj in list(scene.objects):
        if obj != root and obj.parent is None:
            obj.parent = root
    root.location, root.scale = -center * factor, (factor,) * 3
    bpy.context.view_layer.update()

    camera_data = bpy.data.cameras.new('IconCamera')
    camera = bpy.data.objects.new('IconCamera', camera_data)
    scene.collection.objects.link(camera)
    camera.location = (4, -6, 4.5)
    point_at(camera, (0, 0, 0))
    camera_data.type = 'ORTHO'
    scene.camera = camera
    bpy.context.view_layer.update()
    inv = camera.matrix_world.inverted()
    projected = [inv @ (obj.matrix_world @ Vector(corner)) for obj in meshes for corner in obj.bound_box]
    xs, ys = [p.x for p in projected], [p.y for p in projected]
    camera_data.ortho_scale = max(max(xs)-min(xs), max(ys)-min(ys)) / 0.82
    offset = camera.matrix_world.to_quaternion() @ Vector(((min(xs)+max(xs))/2, (min(ys)+max(ys))/2, 0))
    camera.location += offset
    add_light('Key', (-3, -4, 5), 550, 4, (1, 0.94, 0.86))
    add_light('Fill', (4, -1, 2), 330, 3, (0.8, 0.9, 1))
    add_light('Rim', (0, 3, 4), 650, 3, (1, 1, 1))
    world = bpy.data.worlds.new('Studio')
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.45, 0.5, 0.6, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.5
    scene.world = world
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.transparent_max_bounces = 16
    scene.cycles.transmission_bounces = 12
    scene.render.threads_mode, scene.render.threads = 'FIXED', 12
    scene.render.resolution_x = scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.view_settings.view_transform = 'AgX'
    target = project / 'Assets/Resources/Item' / f'Item{item_id}.png'
    scene.render.filepath = str(target)
    bpy.ops.render.render(write_still=True)
    print('ITEM_ICON_DONE ' + json.dumps({'id': item_id, 'path': str(target), 'model': model_name}), flush=True)


def main():
    """Read explicit project/output settings and render only the configured item set."""
    parser = argparse.ArgumentParser()
    parser.add_argument('--project', required=True)
    parser.add_argument('--ids', nargs='*', type=int, default=list(ITEMS))
    parser.add_argument('--samples', type=int, default=128)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    project = Path(args.project).resolve()
    if project.drive.upper() == 'C:':
        raise ValueError('Project and rendered output must be on the E: workspace.')
    for item_id in args.ids:
        render_item(project, item_id, ITEMS[item_id], args.samples)


if __name__ == '__main__':
    main()
