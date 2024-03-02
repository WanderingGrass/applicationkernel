using Jil;
using System.Text;

namespace Todd.ApplicationKernel.Redis.Serializers;
public class JilSerializer : ISerializer
{
    private static readonly Encoding encoding = Encoding.UTF8;

    /// <summary>
    /// Initializes a new instance of the <see cref="JilSerializer"/> class.
    /// </summary>
    public JilSerializer()
        : this(new(
            true,
            false,
            false,
            Jil.DateTimeFormat.ISO8601,
            true,
            UnspecifiedDateTimeKindBehavior.IsLocal))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JilSerializer"/> class.
    /// </summary>
    public JilSerializer(Jil.Options options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        JSON.SetDefaultOptions(options);
    }

    /// <inheritdoc/>
    public byte[] Serialize<T>(T? item)
    {
        var jsonString = JSON.Serialize(item);
        return encoding.GetBytes(jsonString);
    }

    /// <inheritdoc/>
    public T Deserialize<T>(byte[] serializedObject)
    {
        var jsonString = encoding.GetString(serializedObject);
        return JSON.Deserialize<T>(jsonString);
    }
}
