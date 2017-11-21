﻿//Apache2, 2012, Hernan J. González, (https://github.com/leonbloy)
//Apache2, 2017, WinterDev
using System;


namespace Hjg.Pngcs.Chunks
{
    class PngChunkSkipped : PngChunk
    {
        internal PngChunkSkipped(String id, ImageInfo imgInfo, int clen)
            : base(id, imgInfo)
        {
            this.Length = clen;
        }

        public sealed override bool AllowsMultiple()
        {
            return true;
        }

        public sealed override ChunkRaw CreateRawChunk()
        {
            throw new PngjException("Non supported for a skipped chunk");
        }

        public sealed override void ParseFromRaw(ChunkRaw c)
        {
            throw new PngjException("Non supported for a skipped chunk");
        }

        public sealed override void CloneDataFromRead(PngChunk other)
        {
            throw new PngjException("Non supported for a skipped chunk");
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
        {
            return ChunkOrderingConstraint.NONE;
        }



    }
}
