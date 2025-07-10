# Data Visualization API

API for managing data visualization dashboards.

## Dashboard Management

### Delete Dashboard

Deletes a dashboard and all its associated items.

**Endpoint:** `DELETE /api/dashboard/{id}`

**Authentication:** Required (Bearer Token)

**Parameters:**

-   `id` (path parameter): The ID of the dashboard to delete

**Response:**

Success (200):

```json
{
    "message": "Dashboard deleted successfully",
    "deletedDashboardId": 1
}
```

Error Responses:

-   `400 Bad Request`: Invalid dashboard ID
-   `401 Unauthorized`: User not authenticated
-   `404 Not Found`: Dashboard not found
-   `500 Internal Server Error`: Server error

**Example Request:**

```bash
curl -X DELETE "https://localhost:7001/api/dashboard/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Security Features:**

-   User can only delete their own dashboards
-   Cascade delete removes all dashboard items
-   Comprehensive logging for audit trail
-   Input validation and error handling

**Notes:**

-   This operation is irreversible
-   All dashboard items will be permanently deleted
-   The operation is logged for security and audit purposes
