Thank you for downloading!

This package includes a number of free tessellation shaders.
Please note tessellation shaders require DirectX11 (must be supported and enabled) or OpenGL 4.5 (theoretically).
If you are using a newer version of Unity (2018+), chances are these would work under Metal.  

Included shaders are: Legacy (Unity 4.x; check zip) and Standard PBS (Unity 5.x (pre 5.6); check zip) versions  of:
No GPU - does not tessellate on the GPU. May be used to displace dense meshes.
Fixed - tessellation by fixed factor. Always stays the same.  
Distance Based - tessellate by distance. Closer patches get denser.
Edge Length - tessellate based on edge length. Basically a better version of Distance Based.
Smooth - Phong - smooth the surface of an object. Good for making low poly objects smoother.
Smooth Displace - smooth the surface and allows for further displacement.  

Newer versions are completely based on the unity Standard Shader and require Unity 5.6+. These are 
Pimped - This shader auto-pimps your materials without the need for pbr textures
Standard - A standard unity shader with tessellation, instead of parallax. 