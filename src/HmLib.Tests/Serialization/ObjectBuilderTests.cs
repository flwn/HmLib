using Shouldly;

namespace HmLib.Tests.Serialization
{
    using HmLib.Serialization;

    public class ObjectBuilderTests
    {

        public void MustBuildArrays()
        {
            var builder = new ObjectBuilder();

            builder.BeginArray(1);
            builder.BeginItem();
            builder.WriteStringValue("TEST");
            builder.EndItem();
            builder.EndArray();

            builder.CollectionResult.ShouldNotBe(null);

            builder.CollectionResult.ShouldContain("TEST");

            builder.StructResult.ShouldBe(null);
        }
    }
}
