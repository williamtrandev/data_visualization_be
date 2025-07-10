# REST API Import Feature

Tính năng import dữ liệu từ RESTful API vào hệ thống Data Visualization.

## Tổng quan

Tính năng này cho phép bạn import dữ liệu từ bất kỳ RESTful API nào vào hệ thống để tạo dashboard và biểu đồ. Hỗ trợ nhiều định dạng JSON và các tùy chọn cấu hình linh hoạt.

## API Endpoint

**POST** `/api/datasets/import/api`

**Authentication:** Required (Bearer Token)

## Request Body

```json
{
    "datasetName": "Tên Dataset",
    "apiUrl": "https://api.example.com/data",
    "options": {
        "httpMethod": "GET",
        "headers": {},
        "queryParameters": {},
        "requestBody": "",
        "dataPath": "",
        "maxRecords": 1000,
        "timeoutSeconds": 30,
        "flattenNestedObjects": true
    }
}
```

### Parameters

| Parameter     | Type   | Required | Description             |
| ------------- | ------ | -------- | ----------------------- |
| `datasetName` | string | Yes      | Tên dataset sẽ được tạo |
| `apiUrl`      | string | Yes      | URL của REST API        |
| `options`     | object | No       | Các tùy chọn cấu hình   |

### Options

| Option                 | Type    | Default | Description                                     |
| ---------------------- | ------- | ------- | ----------------------------------------------- |
| `httpMethod`           | string  | "GET"   | HTTP method (GET, POST, PUT, DELETE)            |
| `headers`              | object  | {}      | Custom HTTP headers                             |
| `queryParameters`      | object  | {}      | Query parameters cho URL                        |
| `requestBody`          | string  | ""      | Request body cho POST/PUT requests              |
| `dataPath`             | string  | ""      | JSON path để extract data (ví dụ: "data.items") |
| `maxRecords`           | number  | 1000    | Số lượng records tối đa sẽ import               |
| `timeoutSeconds`       | number  | 30      | Timeout cho HTTP request (giây)                 |
| `flattenNestedObjects` | boolean | true    | Có flatten nested objects không                 |

## Response

### Success (200)

```json
{
    "datasetId": 123,
    "status": "Success",
    "message": "Data imported successfully from API. 100 records imported."
}
```

### Error (400/500)

```json
{
    "status": "Error",
    "message": "Failed to import data from API: [error details]"
}
```

## Ví dụ sử dụng

### 1. JSONPlaceholder API (Posts)

```bash
curl -X POST "https://localhost:7001/api/datasets/import/api" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "datasetName": "JSONPlaceholder Posts",
    "apiUrl": "https://jsonplaceholder.typicode.com/posts",
    "options": {
      "httpMethod": "GET",
      "maxRecords": 100,
      "timeoutSeconds": 30,
      "flattenNestedObjects": true
    }
  }'
```

### 2. Random User API

```bash
curl -X POST "https://localhost:7001/api/datasets/import/api" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "datasetName": "Random Users",
    "apiUrl": "https://randomuser.me/api/",
    "options": {
      "httpMethod": "GET",
      "queryParameters": {
        "results": "50",
        "nat": "us,gb,ca"
      },
      "dataPath": "results",
      "maxRecords": 50,
      "timeoutSeconds": 30,
      "flattenNestedObjects": true
    }
  }'
```

### 3. Countries API

```bash
curl -X POST "https://localhost:7001/api/datasets/import/api" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "datasetName": "Countries Data",
    "apiUrl": "https://restcountries.com/v3.1/all",
    "options": {
      "httpMethod": "GET",
      "maxRecords": 200,
      "timeoutSeconds": 60,
      "flattenNestedObjects": true
    }
  }'
```

### 4. GitHub API (với headers)

```bash
curl -X POST "https://localhost:7001/api/datasets/import/api" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "datasetName": "GitHub Repositories",
    "apiUrl": "https://api.github.com/repositories",
    "options": {
      "httpMethod": "GET",
      "headers": {
        "User-Agent": "DataVisualizationAPI/1.0"
      },
      "maxRecords": 100,
      "timeoutSeconds": 30,
      "flattenNestedObjects": true
    }
  }'
```

## Các API mẫu miễn phí

### 1. JSONPlaceholder

-   **URL:** `https://jsonplaceholder.typicode.com/posts`
-   **Mô tả:** API test với 100 posts
-   **Data:** Array of objects với fields: id, userId, title, body

### 2. Random User API

-   **URL:** `https://randomuser.me/api/`
-   **Mô tả:** API tạo user ngẫu nhiên
-   **Data:** Object với field "results" chứa array users
-   **Query params:** results, nat, gender, etc.

### 3. Countries API

-   **URL:** `https://restcountries.com/v3.1/all`
-   **Mô tả:** Thông tin tất cả quốc gia
-   **Data:** Array of country objects
-   **Features:** Nested objects (languages, currencies, etc.)

### 4. GitHub API

-   **URL:** `https://api.github.com/repositories`
-   **Mô tả:** Danh sách repositories trên GitHub
-   **Data:** Array of repository objects
-   **Note:** Cần User-Agent header

### 5. Pokemon API

-   **URL:** `https://pokeapi.co/api/v2/pokemon`
-   **Mô tả:** Thông tin Pokemon
-   **Data:** Object với field "results" chứa array Pokemon

## JSON Path Examples

### Basic Array

```json
{
    "data": [
        { "id": 1, "name": "Item 1" },
        { "id": 2, "name": "Item 2" }
    ]
}
```

**Data Path:** `data`

### Nested Object

```json
{
    "response": {
        "status": "success",
        "data": {
            "items": [{ "id": 1, "name": "Item 1" }]
        }
    }
}
```

**Data Path:** `response.data.items`

### Single Object

```json
{
    "user": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com"
    }
}
```

**Data Path:** (không cần, sẽ tự động wrap thành array)

## Flattening Nested Objects

Khi `flattenNestedObjects: true`, các nested objects sẽ được flatten:

**Input:**

```json
{
    "id": 1,
    "name": "John",
    "address": {
        "street": "123 Main St",
        "city": "New York"
    }
}
```

**Output:**

```json
{
    "id": 1,
    "name": "John",
    "address_street": "123 Main St",
    "address_city": "New York"
}
```

## Error Handling

### Common Errors

1. **Network Error**

    - Timeout
    - Connection refused
    - DNS resolution failed

2. **API Error**

    - 404 Not Found
    - 401 Unauthorized
    - 500 Internal Server Error

3. **Data Error**
    - Invalid JSON response
    - Data path not found
    - Empty response

### Error Response Examples

```json
{
    "status": "Error",
    "message": "Failed to fetch data from API: 404 - Not Found"
}
```

```json
{
    "status": "Error",
    "message": "Data path 'results' not found in API response"
}
```

## Best Practices

1. **Rate Limiting:** Kiểm tra rate limits của API
2. **Timeout:** Đặt timeout phù hợp (30-60 giây)
3. **Max Records:** Giới hạn số records để tránh quá tải
4. **Error Handling:** Luôn có fallback cho API errors
5. **Data Validation:** Validate data trước khi import
6. **Caching:** Cache API responses nếu có thể

## Security Considerations

1. **Authentication:** Sử dụng API keys khi cần thiết
2. **HTTPS:** Luôn sử dụng HTTPS cho production
3. **Input Validation:** Validate URL và parameters
4. **Rate Limiting:** Implement rate limiting cho API calls
5. **Error Logging:** Log errors nhưng không expose sensitive data

## Monitoring

-   Log tất cả API calls
-   Monitor response times
-   Track success/failure rates
-   Alert on repeated failures
-   Monitor data quality metrics
