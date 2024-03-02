﻿namespace Todd.ApplicationKernel.Redis;
public interface ISerializer
{
    /// <summary>
    /// Serializes the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Return the serialized object</returns>
    byte[] Serialize<T>(T? item);

    /// <summary>
    /// Deserializes the specified bytes.
    /// </summary>
    /// <typeparam name="T">The type of the expected object.</typeparam>
    /// <param name="serializedObject">The serialized object.</param>
    /// <returns>
    /// The instance of the specified Item
    /// </returns>
    T? Deserialize<T>(byte[] serializedObject);
}
