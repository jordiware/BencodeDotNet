namespace Jordiware.BencodeDotNet;

/// <summary>
/// Represents the base exception type for all Bencode-related errors.
/// </summary>
/// <remarks>
/// Consumers may catch this exception to handle any error originating
/// from the BencodeDotNet library.
/// </remarks>
public class BencodeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeException"/> class.
    /// </summary>
    /// <param name="message">A message describing the error.</param>
    public BencodeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public BencodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents errors caused by malformed or invalid Bencode data.
/// </summary>
/// <remarks>
/// This exception is thrown when the input does not conform to the
/// Bencode grammar or structure.
/// </remarks>
public sealed class BencodeFormatException : BencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException"/> class.
    /// </summary>
    /// <param name="message">A message describing the format error.</param>
    public BencodeFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the format error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public BencodeFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents errors caused by violations of <see cref="BencodeOptions"/>.
/// </summary>
/// <remarks>
/// This exception indicates that the Bencode data is structurally valid
/// but violates configured limits or validation rules.
/// </remarks>
public sealed class BencodeValidationException : BencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeValidationException"/> class.
    /// </summary>
    /// <param name="message">A message describing the validation error.</param>
    public BencodeValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeValidationException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the validation error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public BencodeValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents I/O-related errors that occur during Bencode reading or writing operations.
/// </summary>
/// <remarks>
/// This exception wraps underlying <see cref="IOException"/> instances
/// encountered while processing streams or files.
/// </remarks>
public sealed class BencodeIOException : BencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeIOException"/> class.
    /// </summary>
    /// <param name="message">A message describing the I/O error.</param>
    public BencodeIOException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeIOException"/> class
    /// with a specified error message and underlying <see cref="IOException"/>.
    /// </summary>
    /// <param name="message">A message describing the I/O error.</param>
    /// <param name="innerException">The underlying <see cref="IOException"/>.</param>
    public BencodeIOException(string message, IOException innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents errors that occur during Bencode serialization or deserialization.
/// </summary>
/// <remarks>
/// This includes failures in serializer logic, unsupported types,
/// or invalid object graphs.
/// </remarks>
public class BencodeSerializerException : BencodeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerException"/> class.
    /// </summary>
    /// <param name="message">A message describing the serialization error.</param>
    public BencodeSerializerException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the serialization error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public BencodeSerializerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents an error caused by the absence of a compatible Bencode serializer.
/// </summary>
/// <remarks>
/// This exception is thrown when no serializer can be resolved for a given type.
/// </remarks>
public sealed class BencodeSerializerNotFoundException : BencodeSerializerException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerNotFoundException"/> class.
    /// </summary>
    /// <param name="message">A message describing the missing serializer.</param>
    public BencodeSerializerNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerNotFoundException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message describing the missing serializer.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public BencodeSerializerNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
