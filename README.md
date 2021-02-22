**MIT License. Do whatever you want.** 

Since this was depreciated/removed from the asset store, and I don't have time to really develop or support this asset anymore I've decided to release it as open source. Code is supplied as is, but if you want to make pull requests I'd be happy to consider them. Take the DMMap folder and stick it somewhere in your unity project's "Assets" folder to install.

See the unity forums thread for more info about DMMap: https://forum.unity.com/threads/released-dmmap-minimap-system-2-0-procedural-multi-layer-vector-like-maps-ugui-25-off.261178

## WHAT 

DMMap is a minimap solution for easily creating vector-looking minimaps, simply draw a few shapes with the DMMapShape component, or throw some shapes into your prefabs as you're building your levels, hit the generate button and you're good to go!

# -= Features =-
Unity 5 Support!  Now uses uGUI for all rendering!
**Works with procedurally generated levels!**
**Have multiple floors in your level?  Not a problem!** 
Support for top down, and side view setup.  (XY, XZ, YZ planes)
Vector like minimap with configurable outlines and colours
**Runtime or editor map generation, works great for both procedural and non-procedural levels!**
Map Overlays and masks (masks require RenderTextures in Unity 4.6)
Transparent map backgrounds
Icons (configurable with tint, icons textures, scale modes, and rotation)
Directional icons that stick to the edges of the screen pointing the player in right direction!
Waypoints! Click on the map to add a marker (marker is added in both worldspace and on the map)
Zooming, Rotating, Center map on Target
Maps are customized with configs.  Make as few or as many configs as you want and switch between them easily (one for minimap, one for fullscreen map, etc)
Additive and subtractive map shapes

# -= Why this map system? =-

I've taken brief looks at other map systems on the asset store, but none of which seemed to suit my needs.  I was looking for a way to render "vector" looking maps, without having to go through the long and boring process of:
Take a top-down screenshot of my level after it's been generated
Bring it into Photoshop and draw my map.
Bring it back into unity and set it up as a map...
That's too much work!
Not to mention that method requires the maps to actually be laid out at edit time.  What about maps procedurally generated at runtime? The above method simply wouldn't work.  DMMap fixes this problem by generating the maps for you, without wasting your valuable time drawing lines in Photoshop.  Simply draw some shapes with the DMMapShape component inside your prefabs, and whether the levels are laid out, or generated at runtime at the click of a button you'll have everything generated!

