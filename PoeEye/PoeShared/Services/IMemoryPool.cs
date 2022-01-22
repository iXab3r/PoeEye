﻿namespace PoeShared.Services;

public interface IMemoryPool
{
    byte[] Rent(int minimumLength);

    /// <summary>
    ///   Returns array to memory pool, DOES NOT CLEAR ARRAY
    /// </summary>
    /// <param name="array"></param>
    void Return(byte[] array);
}