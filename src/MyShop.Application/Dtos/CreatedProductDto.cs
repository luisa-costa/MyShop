using System.Text.Json.Serialization;

namespace MyShop.Application.Dtos;

public record CreatedProductDto([property: JsonPropertyName("id")] Guid Id);