// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Todd.ApplicationKernel;
/// <summary>
/// 当应用于类型时，指定该类型需要被序列化并应生成序列化代码
/// </summary>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public sealed class GenerateSerializerAttribute : Attribute
{
    /// <summary>
    /// 获取或设置是否自动包含主构造函数参数作为可序列化字段
    /// 默认为：<see langword="true"/>（对于<see langword="record"/>类型），其他类型为<see langword="false"/>
    /// </summary>
    public bool IncludePrimaryConstructorParameters { get; init; }

    /// <summary>
    /// 获取或设置应用自动分配字段ID的方式。默认行为是不自动分配字段ID
    /// </summary>
    public GenerateFieldIds GenerateFieldIds { get; init; } = GenerateFieldIds.None;
}

/// <summary>
/// 当应用于类型时，表示该类型是一个拷贝器并应自动注册
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RegisterCopierAttribute : Attribute
{
}

/// <summary>
/// 提供从一个类型填充到另一个类型的功能
/// </summary>
public interface IPopulator<TValue, TSurrogate> where TSurrogate : struct where TValue : class
{
    /// <summary>
    /// 使用<paramref name="surrogate"/>中的值填充<paramref name="value"/>
    /// </summary>
    /// <param name="surrogate">代理对象</param>
    /// <param name="value">目标值</param>
    void Populate(in TSurrogate surrogate, TValue value);
}

/// <summary>
/// 提供两个类型之间相互转换的功能
/// </summary>
public interface IConverter<TValue, TSurrogate> where TSurrogate : struct
{
    /// <summary>
    /// 将代理值转换为实际值类型
    /// </summary>
    /// <param name="surrogate">代理对象</param>
    /// <returns>实际值</returns>
    TValue ConvertFromSurrogate(in TSurrogate surrogate);

    /// <summary>
    /// 将实际值转换为代理类型
    /// </summary>
    /// <param name="value">实际值</param>
    /// <returns>代理对象</returns>
    TSurrogate ConvertToSurrogate(in TValue value);
}

/// <summary>
/// 表示应用该特性的类型、类型成员、参数或返回值应被视为不可变，因此不需要防御性拷贝
/// 当应用于非密封类时，派生类型不保证不可变
/// </summary>
/// <seealso cref="System.Attribute" />
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public sealed class ImmutableAttribute : Attribute
{
}


