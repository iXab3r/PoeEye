﻿namespace PoeShared.GCLog;

internal struct HeapDetails
{
    public long Gen0Size { get; set; }
    public long Gen1Size { get; set; }
    public long Gen2Size { get; set; }
    public long LohSize { get; set; }
}