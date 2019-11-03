using System.IO;

using OpenTK.Graphics.OpenGL4;

using static OpenTK.Graphics.OpenGL4.GL;

namespace Lesson_Builder
{
    public class ShaderProgram : GLHandle
    {
        public static ShaderProgram operator +(ShaderProgram program, Shader shader)
        {
            program.Attach(shader);
            return program;
        }

        public static ShaderProgram operator -(ShaderProgram program, Shader shader)
        {
            program.Detach(shader);
            return program;
        }

        public ShaderProgram()
            : base(CreateProgram()) { }

        public void Attach(Shader shader)
        {
            AttachShader(this, shader);
        }

        public void Detach(Shader shader)
        {
            DetachShader(this, shader);
        }

        public void Link()
        {
            LinkProgram(this);
        }

        public string Validate()
        {
            ValidateProgram(this);
            return InfoLog;
        }

        protected override void OnDispose(bool disposing)
        {
            DeleteProgram(this);
        }

        public override void Enable()
        {
            base.Enable();
            UseProgram(this);
        }

        public int[] AttachedShaders
        {
            get
            {
                var shaders = new int[AttachedShaderCount];
                GetAttachedShaders(this, shaders.Length, out var _, shaders);
                return shaders;
            }
        }

        public void SetAttributeLocation(int index, string name)
        {
            BindAttribLocation(this, index, name);
        }

        public int GetAttributeLocation(string name)
        {
            return GetAttribLocation(this, name);
        }

        public int GetUniformLocation(string name)
        {
            return GL.GetUniformLocation(this, name);
        }

        public string InfoLog
        {
            get
            {
                return GetProgramInfoLog(this);
            }
        }

        public (ActiveAttribType type, int size, string value) GetActiveAttribute(int index)
        {
            var value = GetActiveAttrib(this, index, out var size, out var type);
            return (type, size, value);
        }

        public (ActiveUniformType type, int size, string value) GetActiveUniformX(int index)
        {
            var value = GetActiveUniform(this, index, out var size, out var type);
            return (type, size, value);
        }

        private int GetProgramInfo(GetProgramParameterName name)
        {
            GetProgram(this, name, out var value);
            return value;
        }

        public int MaxGeometryVerticesOut
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.GeometryVerticesOut);
            }
        }

        public int GeometryInputType
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.GeometryInputType);
            }
        }

        public int GeometryOutputType
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.GeometryOutputType);
            }
        }

        public int MaxActiveUniformBlockNameLength
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveUniformBlockMaxNameLength);
            }
        }

        public int ActiveUniformBlocks
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveUniformBlocks);
            }
        }

        public bool IsDeleted
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.DeleteStatus) == 1;
            }
        }

        public bool IsLinked
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.LinkStatus) == 1;
            }
        }

        public bool IsValidated
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ValidateStatus) == 1;
            }
        }

        public int InfoLogLength
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.InfoLogLength);
            }
        }

        public int AttachedShaderCount
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.AttachedShaders);
            }
        }

        public int ActiveUniformsCount
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveUniforms);
            }
        }

        public int MaxActiveUniformNameLength
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveUniformMaxLength);
            }
        }

        public int ActiveAttributesCount
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveAttributes);
            }
        }

        public int MaxActiveAttributeNameLength
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveAttributeMaxLength);
            }
        }

        public int MaxTransformFeedbackVaryingNameLength
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.TransformFeedbackVaryingMaxLength);
            }
        }

        public TransformFeedbackMode TransformFeedbackBufferMode
        {
            get
            {
                return (TransformFeedbackMode)GetProgramInfo(GetProgramParameterName.TransformFeedbackBufferMode);
            }
        }

        public int TransformFeedbackVaryingsCount
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.TransformFeedbackVaryings);
            }
        }

        public int MaxComputeWorkGroupSize
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.MaxComputeWorkGroupSize);
            }
        }

        public int ActiveAtomicCounterBuffersCount
        {
            get
            {
                return GetProgramInfo(GetProgramParameterName.ActiveAtomicCounterBuffers);
            }
        }
    }
}
