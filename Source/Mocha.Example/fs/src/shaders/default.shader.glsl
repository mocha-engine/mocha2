Meta 
{
  name = "Triangle";
  version = "1.0";
  description = "A simple triangle";
}

Vertex 
{
  layout(location = 0) in vec2 inPosition;
  layout(location = 1) in vec2 inTexCoord;

  layout(location = 0) out vec2 texCoord;

  void main() {
    gl_Position = vec4(inPosition, 0.0, 1.0);
    texCoord = inTexCoord;
  }
}

Fragment 
{
  layout(location = 0) in vec2 texCoord;
  layout(location = 0) out vec4 outColor;

  layout(binding = 0) uniform sampler2D textureSampler;

  void main() {
    outColor = texture(textureSampler, texCoord);
  }
}
