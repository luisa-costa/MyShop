# Postman Collection - MyShop API

Esta pasta contém a collection do Postman para testar a API MyShop manualmente.

## 📥 Como Importar no Postman

1. Abra o Postman
2. Clique em **Import** (canto superior esquerdo)
3. Selecione o arquivo `MyShop.postman_collection.json`
4. A collection será importada com todos os endpoints

## 🔧 Configuração

### Variável baseUrl

A collection usa uma variável `baseUrl` que deve ser configurada com a URL da sua API.

**Valor padrão:** `https://localhost:5001`

**Para alterar:**
1. Clique na collection "MyShop API"
2. Vá na aba **Variables**
3. Altere o valor de `baseUrl` para a URL da sua API
   - Desenvolvimento local: `https://localhost:5001` ou `http://localhost:5000`
   - Outro ambiente: `https://api.seudominio.com`

## 📋 Endpoints Disponíveis

### Products

- **GET** `/api/products` - Lista todos os produtos ativos
- **GET** `/api/products/{id}` - Obtém um produto por ID
- **POST** `/api/products` - Cria um novo produto
- **PUT** `/api/products/{id}/stock` - Atualiza o estoque de um produto

### Orders

- **GET** `/api/orders` - Lista todos os pedidos
- **GET** `/api/orders/{id}` - Obtém um pedido por ID
- **POST** `/api/orders` - Cria um novo pedido
- **POST** `/api/orders/{id}/cancel` - Cancela um pedido

## 🧪 Exemplos de Testes no Postman

### Teste Básico de Status Code

No Postman, você pode adicionar scripts de teste na aba **Tests**:

```javascript
// Verifica se o status code é 200
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});
```

### Teste de Body

```javascript
// Verifica se a resposta contém um produto
pm.test("Response contains product", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('name');
    pm.expect(jsonData.name).to.be.a('string');
});
```

### Teste de Criação

```javascript
// Testa criação de produto
pm.test("Product created successfully", function () {
    pm.response.to.have.status(201);
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('id');
    pm.expect(jsonData.id).to.be.a('number');
});
```

## 🔄 Diferença entre Postman e Testes Automatizados

### Postman (Testes Manuais)
- ✅ Interface visual amigável
- ✅ Fácil de compartilhar
- ✅ Permite exploração manual
- ❌ Requer execução manual
- ❌ Não integrado ao CI/CD

### Testes Automatizados (C#)
- ✅ Executáveis automaticamente
- ✅ Integrados ao CI/CD
- ✅ Mais rápidos para grandes suites
- ❌ Requer conhecimento de C#
- ❌ Menos visual

**Recomendação:** Use ambos! Postman para exploração e documentação, testes automatizados para garantir qualidade.

## 📚 Recursos

- [Documentação do Postman](https://learning.postman.com/)
- [Scripts de Teste no Postman](https://learning.postman.com/docs/writing-scripts/test-scripts/)

