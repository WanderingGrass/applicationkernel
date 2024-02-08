namespace Todd.ApplicationKernel.Base.Types;
public interface IIdentifiable<out T>
{
    T Id { get; }
}