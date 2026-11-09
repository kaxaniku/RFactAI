using RFactAI.Application.Facts;
using RFactAI.Domain.Exceptions;

namespace RFactAI.Application.Tests;

public class FactJsonParserTests
{
    [Fact]
    public void Parses_valid_json()
    {
        var result = FactJsonParser.Parse("""{"category":"space","facts":["A","B"]}""", "space", 5);

        Assert.Equal("space", result.Category);
        Assert.Equal(["A", "B"], result.Facts);
    }

    [Fact]
    public void Strips_markdown_fences()
    {
        var result = FactJsonParser.Parse("```json\n{\"category\":\"space\",\"facts\":[\"A\"]}\n```", "space", 1);

        Assert.Equal(["A"], result.Facts);
    }

    [Fact]
    public void Trims_extra_facts_to_requested_count()
    {
        var result = FactJsonParser.Parse("""{"category":"c","facts":["1","2","3","4"]}""", "c", 2);

        Assert.Equal(["1", "2"], result.Facts);
    }

    [Fact]
    public void Ignores_extra_properties_and_blank_facts()
    {
        var result = FactJsonParser.Parse("""{"category":"c","facts":["  ","A"],"note":"x"}""", "c", 3);

        Assert.Equal(["A"], result.Facts);
    }

    [Fact]
    public void Uses_requested_category_when_model_omits_it()
    {
        var result = FactJsonParser.Parse("""{"facts":["A"]}""", "requested", 1);

        Assert.Equal("requested", result.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sure! Here are some facts about space.")]
    [InlineData("""{"category":"c","facts":[]}""")]
    [InlineData("""{"category":"c","facts":["",""]}""")]
    [InlineData("""{"category":"c","facts":"not a list"}""")]
    [InlineData("""["A","B"]""")]
    [InlineData("null")]
    public void Throws_on_unusable_output(string? text)
    {
        Assert.Throws<InvalidLlmResponseException>(() => FactJsonParser.Parse(text, "c", 3));
    }
}
