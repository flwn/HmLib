namespace HmLib.Serialization
{
    internal interface IHasResult<T>
    {
        T Result
        {
            get;
        }
    }
}
