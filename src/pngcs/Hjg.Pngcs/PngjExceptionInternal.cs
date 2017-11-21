﻿//Apache2, 2012, Hernan J. González, (https://github.com/leonbloy)
//Apache2, 2017, WinterDev
namespace Hjg.Pngcs
{

    using System;

    /// <summary>
    /// Exception for internal problems
    /// </summary>
    [Serializable]
    public class PngjExceptionInternal : Exception
    {
        private const long serialVersionUID = 1L;

        public PngjExceptionInternal()
            : base()
        {
        }

        public PngjExceptionInternal(String message, Exception cause)
            : base(message, cause)
        {
        }

        public PngjExceptionInternal(String message)
            : base(message)
        {
        }

        public PngjExceptionInternal(Exception cause)
            : base(cause.Message, cause)
        {
        }
    }
}
