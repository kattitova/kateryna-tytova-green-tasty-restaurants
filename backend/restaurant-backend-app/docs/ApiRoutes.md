# API Routes

## Auth

### `POST` /register
```json
{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john_doe@mail.com",
    "password": "1234SecurePassword!",
    "confirmPassword": "1234SecurePassword!"
}
```

Response: 
`200 OK`
```json
{
    "message":"User registered and confirmed successfully"
}
```

### `POST` /signin
```json
{
    "email": "john_doe@mail.com",
    "password": "1234SecurePassword!",
}
```

Response: `200 OK`
```json
{
    "message": "Sign in successful",
    "idToken": "token",
    "accessToken": "access token",
}
```

## Feedback

### `GET` /feedback
Query parameters:
- `locationId` (string, required)
- `type` (Service | CuisineExperience)
- `sort` (TopRatedFirst | LowRatedFirst | NewestFirst | OldestFirst)
- `page` (integer, required)

Response: `200 OK`
```json
{
  "items": [
    {
      "reservationId": "string",
      "locationId": "string",
      "userId": "string|null",
      "comment": "string|null",
      "rating": 5,
      "createdAt": "2026-01-01T12:00:00Z",
      "type": "string",
      "username": "string|null"
    }
  ],
  "totalPages": 1,
  "currentPage": 1
}
```

### `POST` /feedback
Request body:
```json
{
  "reservationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "type": "Service | CuisineExperience"
}
```

Response: `200 OK`
```json
{
  "reservationId": "string",
  "locationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "createdAt": "2026-01-01T12:00:00Z",
  "type": "string",
  "username": "string|null"
}
```

### `PUT` /feedback
Request body:
```json
{
  "reservationId": "string",
  "userId": "string|null",
  "comment": "string|null",
  "rating": 5,
  "type": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Feedback updated successfully."
}
```

## Locations

### `POST` /locations
Request body:
```json
{
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageBase64": "string|null",
  "rating": 4.5
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `GET` /locations
Response: `200 OK`
```json
[
  {
    "id": "string",
    "address": "string",
    "description": "string",
    "totalCapacity": 50,
    "averageOccupancy": "string",
    "imageUrl": "string|null",
    "rating": 4.5
  }
]
```

### `GET` /locations/{id}/dishes
Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `DELETE` /locations/{locationId}/dishes/{dishId}
Response: `200 OK`
```json
{
  "message": "Dish removed from location successfully."
}
```


### `GET` /locations/{id}
Response: `200 OK`
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `PUT` /locations/{id}
Request body:
```json
{
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageBase64": "string|null",
  "rating": 4.5
}
```
Response: 200 OK
```json
{
  "id": "string",
  "address": "string",
  "description": "string",
  "totalCapacity": 50,
  "averageOccupancy": "string",
  "imageUrl": "string|null",
  "rating": 4.5
}
```

### `DELETE` /locations/{id}
Response: 200 OK
```json
{
  "message": "Location deleted successfully"
}
```

## Waiters

### `POST` /waiters
Request body:
```json
{
  "email": "waiter@example.com",
  "locationId": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Email added successfully"
}
```

### `GET` /waiters/{userId}/location
Response: `200 OK`
```json
{
  "locationId": "string"
}
```

### `DELETE` /waiters
Request body:
```json
{
  "email": "waiter@example.com",
  "locationId": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Email deleted successfully"
}
```

## Dishes

### `POST` /dishes
Request body:
```json
{
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageBase64": "string|null",
  "state": "string",
  "locationIds": ["string"]
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

### `GET` /dishes
Optional query parameters:
- `dishType` (Appetizer | MainCourse | Dessert)
- `sortBy` (PriceAscending, PriceDescending, PopularityAscending, PopularityDescending)
- `name`

Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `PUT` /dishes/{id}
Request body:
```json
{
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageBase64": "string|null",
  "state": "string"
}
```
Response: 200 OK
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

### `DELETE` /dishes/{id}
Query parameters:
  force (boolean, optional)
Response: 200 OK
```json
{
  "message": "Dish '{id}' deleted successfully."
}
```


### `GET` /dishes/popular
Response: `200 OK`
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "string",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `GET` /dishes/{id}
Response: `200 OK`
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "dishType": "string",
  "price": 12.34,
  "weight": "string",
  "calories": "string",
  "proteins": "string",
  "fats": "string",
  "carbohydrates": "string",
  "vitamins": "string",
  "imageUrl": "string",
  "state": "string"
}
```

## Reservations

### `POST` /reservations
Request body:
```json
{
  "tableId": "string",
  "customerEmail": "string|null",
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "customerId": "string|null"
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "tableId": "string",
  "tableNumber": 3,
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

### `GET` /reservations
Response: `200 OK`
```json
[
  {
    "id": "string",
    "tableId": "string",
    "tableNumber": 3,
    "startTime": "2026-08-07T19:00:00Z",
    "endTime": "2026-08-07T20:30:00Z",
    "guestCount": 4,
    "status": "Reserved",
    "customerId": "string|null",
    "waiterId": "string",
    "locationAddress": "string",
    "restaurantName": "Green & Tasty",
    "hasSubmittedPreOrder": false
  }
]
```

`tableNumber` is the number of the table in its location. It is `null` when the table record is missing. `POST /reservations`, `GET /reservations` and `PUT /reservations` return `tableNumber`. `GET /reservations/{reservationId}` does not return it.

### `GET` /reservations/{reservationId}
Response: `200 OK`
```json
{
  "id": "string",
  "tableId": "string",
  "startTime": "2026-08-07T19:00:00Z",
  "endTime": "2026-08-07T20:30:00Z",
  "guestCount": 4,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

### `PATCH` /reservations/{reservationId}/status
Request body:
```json
{
  "newStatus": "Completed"
}
```

Response: `200 OK`
```json
{
  "message": "Reservation status updated successfully."
}
```

### `GET` /reservations/{reservationId}/invite-token
Response: `200 OK`
```json
{
  "token": "string"
}
```

### `GET` /reservations/{reservationId}/feedback
Response: `200 OK`
```json
{
  "service": {
    "reservationId": "string",
    "locationId": "string",
    "userId": "string|null",
    "comment": "string|null",
    "rating": 5,
    "createdAt": "2026-08-07T21:00:00Z",
    "type": "string",
    "username": "string|null"
  },
  "cuisineExperience": {
    "reservationId": "string",
    "locationId": "string",
    "userId": "string|null",
    "comment": "string|null",
    "rating": 5,
    "createdAt": "2026-08-07T21:00:00Z",
    "type": "string",
    "username": "string|null"
  }
}
```

### `GET` /reservations/{reservationId}/guest-session-id
Auth: Cognito ID token. The caller must be the owner of the reservation.

Response: 200 OK
```json
{
  "guestSessionId": "owner-group-1"
}
```
The value is the prefix `owner-` and then the `groupReservationId` of the group reservation of this reservation.

Errors:

| Status | When | Body |
|---|---|---|
| 400 | The `reservationId` path parameter is missing or empty. | `{"error": "Missing 'reservationId' in path parameters."}` |
| 401 | The token has no `sub` claim, or the claim is empty or contains only white space. | `{"error": "Unauthorized."}` |
| 403 | The caller is not the owner of the reservation. A reservation without a customer has no owner. | `{"error": "Only the reservation owner can perform this action."}` |
| 404 | The reservation does not exist. | `{"error": "Reservation not found."}` |
| 404 | The reservation has no group reservation. | `{"error": "Group reservation was not found"}` |

Notes:
- The first request creates the owner session. Later requests return the same id and do not write.
- This request works in every group reservation status.
- The owner session has no guest session token. Thus no token passes the token check of the `/guest/sessions/{guestSessionId}` endpoints for the owner session id.
- If the token is missing or not valid, the API Gateway Cognito authorizer sends 401 before the Lambda runs. That body comes from API Gateway, not from the table above.


### `DELETE` /reservations
Request body:
```json
{
  "id": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Reservation cancelled successfully"
}
```

The endpoint also sets the active pre-order of the reservation to `Cancelled`. A reservation without a pre-order is not an error.

### `PUT` /reservations
Request body:
```json
{
  "reservationId": "string",
  "startTime": "2026-08-07T19:15:00Z",
  "endTime": "2026-08-07T20:45:00Z",
  "guestCount": 5
}
```

Response: `200 OK`
```json
{
  "id": "string",
  "tableId": "string",
  "tableNumber": 3,
  "startTime": "2026-08-07T19:15:00Z",
  "endTime": "2026-08-07T20:45:00Z",
  "guestCount": 5,
  "status": "Reserved",
  "customerId": "string|null",
  "waiterId": "string",
  "locationAddress": "string",
  "restaurantName": "Green & Tasty",
  "hasSubmittedPreOrder": false
}
```

After the endpoint saves the reservation, it changes the group reservation. This applies only to a reservation that has a customer and the status `Reserved`. Otherwise the group does not change.

| New `guestCount` | Group reservation | Result |
|---|---|---|
| 2 or more | None | A new group with status `Open`. |
| 2 or more | `Closed` | The same group becomes `Open`, with the same `groupReservationId` and token. |
| 2 or more | `Open` or `Submitted` | No change. |
| Less than 2 | `Open` | The group becomes `Closed`. The guest sessions stay. |
| Less than 2 | `Submitted`, `Closed` or none | No change. |

If the group write fails, the endpoint returns 500. The same edit, repeated, repairs the group.

### `PATCH` /reservations/{reservationId}/order/confirmations/{dishId}
Response: `200 OK`
```json
{
  "message": "Pre order confirmed successfully"
}
```

### `PATCH` /reservations/{reservationId}/order/cancellations/{dishId}
Response: `200 OK`
```json
"Pre order dish canceled successfully"
```

### `PATCH` /reservations/{reservationId}/table
Request body:
```json
{
  "tableId": "string"
}
```

Response: `200 OK`
```json
"Your reservation table was updated successfully."
```

## Tables

### `POST` /tables
Request body:
```json
{
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `PUT` /tables/{id}
Request body:
```json
{
  "capacity": 4,
  "tableNumber": 10
}
```
Response: 200 OK
```json
{
  "id": "string",
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `DELETE` /tables/{id}
Response: 200 OK
```json
{
  "message": "Table deleted successfully"
}
```

Response: `201 Created`
```json
{
  "id": "string",
  "locationId": "string",
  "capacity": 4,
  "tableNumber": 10
}
```

### `GET` /available-tables
Query parameters:
- `locationId` (string, optional)
- `capacity` (integer, optional)
- `startTime` (ISO 8601 string, optional)

Response: `200 OK`
```json
[
  {
    "id": "string",
    "locationId": "string",
    "capacity": 4,
    "tableNumber": 10
  }
]
```

### `GET` /locations/{locationId}/tables/all
Response: `200 OK`
```json
[
  {
    "id": "string",
    "locationId": "string",
    "capacity": 4,
    "tableNumber": 10
  }
]
```

### `GET` /tables/{id}/available-time-slots
Query parameters:
- `startTime` (ISO 8601 string, required)

Response: `200 OK`
```json
[
  {
    "start": "2026-08-07T19:00:00Z",
    "end": "2026-08-07T20:30:00Z"
  }
]
```

## User Profile

### `GET` /me
Response: `200 OK`
```json
{
  "id": "string",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "role": "string"
}
```

### `PUT` /me
Request body:
```json
{
  "firstName": "string",
  "lastName": "string"
}
```

Response: `200 OK`
```json
"User information updated successfully."
```

### `PUT` /me/change-password
Request body:
```json
{
  "oldPassword": "string",
  "newPassword": "string",
  "accessToken": "string"
}
```

Response: `200 OK`
```json
{
  "message": "Password changed successfully."
}
```

### `GET` /users
Query parameters:
- `fullName` (string, required)

Response: `200 OK`
```json
[
  {
    "id": "string",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "role": "string"
  }
]
```

### `GET` /users/{id}/profile-image
Response: `200 OK`
```json
{
  "presignedUrl": "https://..."
}
```

If the user has not uploaded an image yet, the route returns:
```json
"Profile image is not uploaded so far."
```

### `PUT` /me/profile-image
Request body:
```json
{
  "base64Image": "string"
}
```

Response: `200 OK`
```json
"Photo was uploaded successfully!"
```

### `GET` /users/{id}/name
Response: `200 OK`
```json
{
  "firstName": "string",
  "lastName": "string"
}
```

## Orders

### `GET` /orders
Response: `200 OK`
```json
[
  {
    "orderId": "string",
    "reservationId": "string",
    "reservationAddress": "string",
    "reservationTime": "string",
    "orderItems": [
      {
        "dish": {
          "id": "string",
          "name": "string",
          "description": "string",
          "dishType": "string",
          "price": 12.34,
          "weight": "string",
          "calories": "string",
          "proteins": "string",
          "fats": "string",
          "carbohydrates": "string",
          "vitamins": "string",
          "imageUrl": "string",
          "state": "string"
        },
        "quantity": 2,
        "state": 0
      }
    ],
    "status": "Created",
    "groupBonusTier": "TenPercentDiscount",
    "discountPercent": 10
  }
]
```

The order response has these fields for the group bonus and the item state:
- `groupBonusTier` is `"TenPercentDiscount"`, `"FreeDessert"` or `null`. `discountPercent` is 10 for `TenPercentDiscount`, and 0 otherwise. Orders from before this change return `null` and 0.
- `orderItems[].state` is the number of the item state: 0 `Pending`, 1 `Confirmed`, 2 `Canceled`. The frontend shows state 2 as "Rejected". The source is `Constants/Orders/OrderItemState.cs`.
- The API does not compute a total. The client applies `discountPercent` to the items that are not `Canceled`.
- Only a group submit sets the bonus (see `POST /guest/groups/{groupReservationId}/submit`). A later change of the order does not compute it again.
- Storage: the order keeps the tier as its name. If the order has no bonus, the write deletes the `groupBonusTier` attribute. A missing attribute reads as `null`.

### `GET` /orders/{reservationId}
Response: `200 OK`
```json
{
  "orderId": "string",
  "reservationId": "string",
  "reservationAddress": "string",
  "reservationTime": "string",
  "orderItems": [
    {
      "dish": {
        "id": "string",
        "name": "string",
        "description": "string",
        "dishType": "string",
        "price": 12.34,
        "weight": "string",
        "calories": "string",
        "proteins": "string",
        "fats": "string",
        "carbohydrates": "string",
        "vitamins": "string",
        "imageUrl": "string",
        "state": "string"
      },
      "quantity": 2,
      "state": 0
    }
  ],
  "status": "Created",
  "groupBonusTier": "TenPercentDiscount",
  "discountPercent": 10
}
```

### `POST` /orders
Request body:
```json
{
  "reservationId": "string",
  "orderItems": [
    {
      "dishId": "string",
      "quantity": 2
    }
  ]
}
```

Response: `201 Created`
```json
{
  "orderId": "string",
  "reservationId": "string",
  "reservationAddress": "string",
  "reservationTime": "string",
  "orderItems": [
    {
      "dish": {
        "id": "string",
        "name": "string",
        "description": "string",
        "dishType": "string",
        "price": 12.34,
        "weight": "string",
        "calories": "string",
        "proteins": "string",
        "fats": "string",
        "carbohydrates": "string",
        "vitamins": "string",
        "imageUrl": "string",
        "state": "string"
      },
      "quantity": 2,
      "state": 0
    }
  ],
  "status": "Created",
  "groupBonusTier": "TenPercentDiscount",
  "discountPercent": 10
}
```

The response has the same fields as `GET /orders/{reservationId}`, including `groupBonusTier`, `discountPercent` and `orderItems[].state`. A new order has `groupBonusTier` `null`, `discountPercent` 0 and the item state 0.

### `PUT` /orders/{reservationId}
Request body:
```json
{
  "orderItems": [
    {
      "dishId": "string",
      "quantity": 3
    }
  ]
}
```

Response: `200 OK`
```json
"Pre-order updated successfully."
```

### `DELETE` /orders/{reservationId}
Response: `200 OK`
```json
"Pre-order canceled successfully."
```

### `POST` /orders/{reservationId}/submit
Response: `200 OK`
```json
"Pre-order submitted successfully."
```

### `POST` /orders/{reservationId}/unsubmit
Response: `200 OK`
```json
"Pre-order unsubmitted successfully."
```

## Reports

### `GET` /reports/download
Query parameters:
- `format` (string, optional: Excel, Pdf, Csv)
- `type` (string, optional: Service, Sales)
- `startDate` (YYYY-MM-DD, optional)
- `endDate` (YYYY-MM-DD, optional)
- `locationId` (string, required)

Response: `200 OK` with file download content

### `GET` /reports/generate
Query parameters:
- `type` (string, optional: Service, Sales)
- `startDate` (YYYY-MM-DD, required)
- `endDate` (YYYY-MM-DD, required)
- `locationId` (string, required)

Response: `200 OK`
```json
[
  {
    "reportType": "Service",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "userId": "string",
    "locationId": "string",
    "locationName": "string",
    "waiterId": "string",
    "waiterName": "string",
    "waiterEmail": "string",
    "workingHours": 72.5,
    "totalOrders": 28,
    "avgServiceFeedback": 4.7,
    "minServiceFeedback": 3.8,
    "deltaAvgServiceFeedback": 1.2,
    "deltaTotalOrders": 5.0
  }
]
```

For `type=Sales`, the response is an array of sales report history objects:
```json
[
  {
    "reportType": "Sales",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "userId": "string",
    "locationId": "string",
    "locationName": "string",
    "totalOrders": 147,
    "avgCuisineFeedback": 4.3,
    "minCuisineFeedback": 2.5,
    "revenue": 32145.60,
    "deltaAvgCuisineFeedback": 2.1,
    "deltaRevenue": 8.4
  }
]
```

### `GET` /reports
Response: `200 OK`
```json
[
  {
    "id": "string",
    "reportType": "Service",
    "startDate": "2026-08-01",
    "endDate": "2026-08-07",
    "locationId": "string",
    "createdAt": "2026-08-07T12:00:00Z"
  }
]
```

## Notifications

### `GET` /notifications
Response: `200 OK`
```json
[
  {
    "id": "string",
    "message": "string",
    "orderId": "string",
    "reservationId": "string",
    "createdAt": "2026-08-07T12:00:00Z",
    "isRead": false
  }
]
```

### `PATCH` /notifications/{id}/read
Response: `200 OK`
```json
"Notification marked as read"
```

## Group & Guest Reservations

### `GET` /guest/invites/{token}
Response: `200 OK`
```json
{
  "groupReservationId": "string",
  "restaurantName": "string",
  "locationAddress": "string",
  "startTime": "2026-08-07T19:00:00Z"
}
```

### `POST` /guest/sessions
Request body:
```json
{
  "groupReservationId": "string",
  "guestName": "string"
}
```
Response: 201 Created
```json
{
  "sessionToken": "string"
}
```

### `GET` /guest/sessions/{guestSessionId}
Response: `200 OK`
```json
{
  "id": "string",
  "groupReservationId": "string",
  "guestName": "string",
  "status": "string"
}
```
### `POST` /guest-sessions/{guestSessionId}/dishes
Request body:
```json
{
  "dishId": "string",
  "quantity": 1
}
```
Response: 200 OK
```json
{
  "message": "Dish added to guest session successfully."
}
```

### `DELETE` /guest-sessions/{guestSessionId}/dishes
Request body:
```json
{
  "dishId": "string"
}
```
Response: 200 OK
```json
{
  "message": "Dish removed from guest session successfully."
}
```

### `GET` /guest/sessions/{guestSessionId}/menu
Response: 200 OK
```json
[
  {
    "id": "string",
    "name": "string",
    "imageUrl": "string",
    "price": 12.34,
    "state": "Available",
    "weight": "string",
    "dishType": "string",
    "description": "string",
    "calories": "string",
    "proteins": "string",
    "fats": "string",
    "carbohydrates": "string",
    "vitamins": "string"
  }
]
```

### `POST` /guest/sessions/{guestSessionId}/submit
Response: 200 OK
```json
{
  "message": "Guest cart submitted successfully."
}
```

### `GET` /guest/groups/{groupReservationId}/status
Auth: none. The route is public: API Gateway has no authorizer on it. Anyone who knows the `groupReservationId` can read the response. The response has no names.

Response: 200 OK
```json
{
  "currentParticipants": 3,
  "maxCapacity": 4,
  "status": "Open",
  "orderedCount": 2,
  "bonus": {
    "state": "InProgress",
    "tier": "TenPercentDiscount",
    "discountPercent": 10,
    "deadline": "2026-11-10T18:00:00Z"
  }
}
```

Fields:
- `currentParticipants`: joined guests plus the owner. Owner sessions are not guests.
- `maxCapacity`: `guestCount` of the reservation. It includes the owner.
- `status`: `Open`, `Submitted` or `Closed`.
- `orderedCount`: 1 if the owner has own items in the order, plus the guest sessions with `isSubmitted: true`. After a group submit, the dishes of the guests are not counted as owner items.
- `bonus.state`: `InProgress`, `Achieved` or `Missed`.
- `bonus.tier`: `TenPercentDiscount`, `FreeDessert` or `null`. `null` only when `state` is `Missed`.
- `bonus.discountPercent`: 10 for `TenPercentDiscount` (also while `state` is `InProgress`), else 0.
- `bonus.deadline`: UTC time with `Z`, or `null` when `state` is `Achieved`.

Bonus rules. `start` is the reservation start time in UTC. The last time is the latest of the owner order `createdAt` and the `submittedAt` of the ordered guests. The rules skip a missing time.
- All seats ordered (`orderedCount` is not less than `maxCapacity`), in every group status:
  - Last time is `start - 24 h` or earlier, or there is no last time: `Achieved` with `TenPercentDiscount`.
  - Else last time is `start - 2 h` or earlier: `Achieved` with `FreeDessert`.
  - Else `Missed`.
- Not all seats ordered and the group status is not `Open`: `Missed`.
- Not all seats ordered and the group status is `Open`:
  - Now is `start - 24 h` or earlier: `InProgress` with `TenPercentDiscount`. The `deadline` is `start - 24 h`.
  - Else now is `start - 2 h` or earlier: `InProgress` with `FreeDessert`. The `deadline` is `start - 2 h`.
  - Else `Missed`.
- `Missed` has `tier: null`, `discountPercent: 0` and `deadline` = `start - 2 h`.
- The limits are inclusive.
- The order has no discount field. The client calculates the discount.

Errors:

| Status | When | Body |
|---|---|---|
| 400 | The `groupReservationId` path parameter is missing or empty. | `{"error": "Missing groupReservationId in path parameters."}` |
| 404 | The group reservation does not exist. | `{"error": "Group reservation was not found"}` |
| 500 | Another error occurs, for example a DynamoDB error, or the parent reservation does not exist. | `{"error": "Internal server error: {message}"}` |

In the 500 body, `{message}` is the text of the error.

### `POST` /guest/groups/{groupReservationId}/submit
Auth: Cognito ID token. The caller must be the owner of the reservation.

Request body:
```json
{
  "reservationId": "reservation-1"
}
```

Response: 200 OK
```json
[
  {
    "dishId": "dish1",
    "quantity": 3
  },
  {
    "dishId": "dish2",
    "quantity": 2
  }
]
```

Errors:

| Status | When | Body |
|---|---|---|
| 400 | The `groupReservationId` path parameter is missing or empty. | `{"error": "Missing 'groupReservationId' in path parameters."}` |
| 401 | The token has no `sub` claim, or the claim is empty or contains only white space. | `{"error": "Unauthorized."}` |
| 400 | The body is missing, empty or `null`. | `{"error": "Body is empty"}` |
| 400 | The body is not valid JSON. | `{"error": "Invalid json body"}` |
| 400 | `reservationId` is missing, `null` or empty. | `{"title": "One or more validation errors occurred.", "errors": {"ReservationId": ["The ReservationId field is required."]}}` |
| 404 | The reservation does not exist. | `{"error": "Reservation not found."}` |
| 403 | The caller is not the owner of the reservation. A reservation without a customer has no owner. | `{"error": "Only the reservation owner can perform this action."}` |
| 404 | The group reservation does not exist, or it belongs to a different reservation. | `{"error": "Group reservation was not found"}` |
| 400 | The group reservation status is `Submitted`. | `{"message": "Group reservation was submitted.", "errorCode": "GroupReservationSubmitted"}` |
| 400 | The group reservation status is `Closed`. | `{"message": "Group reservation is expired.", "errorCode": "GroupReservationExpired"}` |
| 400 | No submitted guest session has dishes, and the owner has no own order items. | `{"message": "Nothing to submit: the group has no pre-orders.", "errorCode": "Unknown"}` |
| 400 | The reservation has no order, the group has dishes to submit, and the table has no location. | `{"message": "Location not found for the reservation's table.", "errorCode": "Unknown"}` |
| 400 | A parallel request changed the group reservation status before the write. | `{"message": "Group reservation was submitted.", "errorCode": "GroupReservationSubmitted"}` |
| 500 | DynamoDB cancels the transaction for a reason other than the status condition, or another error occurs. | `{"error": "Internal Server Error"}` |

The checks run in the order of the table.

Notes:
- The request needs at least one pre-order. A pre-order is a dish in a guest session with `isSubmitted: true`, or an owner order item that is not `Canceled`. The owner items are the order items without the dishes of the submitted guests.
- The request uses only the guest sessions with `isSubmitted: true`. The request also uses the owner session when it has `isSubmitted: true`.
- The request adds the quantities of each `dishId`. The response lists each dish one time, in the order of its first occurrence.
- If the reservation has no order, the request creates the order of the owner. It has `customerId` of the caller, the address of the table location, the reservation time text, the guest dishes as `Pending` items, `createdAt` and the status `SubmittedByCustomer`. The request reads the location only in this case.
- If the reservation has an order, the request adds the guest dishes to it. The owner items are not added a second time:
  - An order item with the same `dishId` and state `Pending` or `Confirmed` gets the quantity added. Its state becomes `Pending`.
  - An order item with the same `dishId` and state `Canceled` gets the quantity from the guest sessions. Its state becomes `Pending`.
  - A dish without an order item becomes a new `Pending` item at the end of the list.
  - Order items for other dishes do not change.
- Before the write, the request computes the group bonus with the rules of the status route, from the order before the merge. If the bonus state is `Achieved`, the order gets `groupBonusTier` and `discountPercent`. Otherwise the order gets `null` and 0, and this replaces a value from before. When there is no bonus, the write deletes the `groupBonusTier` attribute. Later order changes do not compute the bonus again.
- The order status becomes `SubmittedByCustomer`, and the group reservation status becomes `Submitted`. One DynamoDB transaction writes both items, so both change or neither changes.
- The transaction writes the group reservation only if its status is `Open`. A second or parallel submit gets 400 `GroupReservationSubmitted`.
- After the submit, guests cannot change their sessions.
- The response lists only the dishes from the guest sessions. It does not list the other order items. The list is empty (`[]`) when only the owner ordered.
- If the token is missing or not valid, the API Gateway Cognito authorizer sends 401 before the Lambda runs. That body comes from API Gateway, not from the table above.

### `GET` /guest/groups/{groupReservationId}/sessions
Auth: Cognito ID token. The caller must be the owner of the parent reservation.

Response: 200 OK
```json
{
  "participants": [
    {
      "id": "session1",
      "type": "Guest",
      "name": "Andrii",
      "items": [
        {
          "dish": {
            "id": "dish1",
            "name": "Caesar Salad",
            "price": 180
          },
          "quantity": 2
        }
      ],
      "total": 360,
      "isSubmitted": true
    }
  ],
  "total": 360,
  "owner": {
    "id": "owner-group1",
    "type": "Owner",
    "name": "Owner",
    "items": [],
    "total": 0,
    "isSubmitted": false
  }
}
```
The `dish` value is the full dish object. The example shows only `id`, `name` and `price`.

The `owner` participant has the id `owner-{groupReservationId}`, the name `Owner` and the own items of the owner. `isSubmitted` is `true` when the owner has own items. `participants` and `total` list guests only.

Errors:

| Status | When | Body |
|---|---|---|
| 400 | The `groupReservationId` path parameter is missing or empty. | `{"error": "Missing 'groupReservationId' in path parameters."}` |
| 401 | The token has no `sub` claim, or the claim is empty. | `{"error": "Unauthorized."}` |
| 403 | The caller is not the owner of the parent reservation. | `{"error": "Only the reservation owner can perform this action."}` |
| 404 | The group reservation does not exist. | `{"error": "Group reservation was not found"}` |
| 404 | The parent reservation does not exist. | `{"error": "Reservation not found."}` |

Notes:
- `participants` lists only guest sessions. The owner is in `owner`.
- If a dish no longer exists, the response leaves out the order items for that dish. The totals do not include them.
- This request works in every group reservation status.
- If the token is missing or not valid, the API Gateway Cognito authorizer sends 401 before the Lambda runs. That body comes from API Gateway, not from the table above.

### `DELETE` /guest/groups/{groupReservationId}/sessions/{guestSessionId}
Auth: Cognito ID token. The caller must be the owner of the parent reservation.

The request deletes one guest session from the group reservation. The response lists the guest sessions that remain.

Response: 200 OK
```json
{
  "participants": [
    {
      "id": "session2",
      "type": "Guest",
      "name": "Olena",
      "items": [
        {
          "dish": {
            "id": "dish2",
            "name": "Tom Yum",
            "price": 220
          },
          "quantity": 1
        }
      ],
      "total": 220,
      "isSubmitted": false
    }
  ],
  "total": 220,
  "owner": {
    "id": "owner-group1",
    "type": "Owner",
    "name": "Owner",
    "items": [],
    "total": 0,
    "isSubmitted": false
  }
}
```
The `dish` value is the full dish object. The example shows only `id`, `name` and `price`.

The `owner` participant has the same shape as in `GET /guest/groups/{groupReservationId}/sessions`.

Errors:

| Status | When | Body |
|---|---|---|
| 400 | The `groupReservationId` path parameter is missing or empty. | `{"error": "Missing 'groupReservationId' in path parameters."}` |
| 400 | The `guestSessionId` path parameter is missing or empty. | `{"error": "Missing 'guestSessionId' in path parameters."}` |
| 401 | The token has no `sub` claim, or the claim is empty or contains only white space. | `{"error": "Unauthorized."}` |
| 404 | The group reservation does not exist. | `{"error": "Group reservation was not found"}` |
| 404 | The parent reservation does not exist. | `{"error": "Reservation not found."}` |
| 403 | The caller is not the owner of the parent reservation. | `{"error": "Only the reservation owner can perform this action."}` |
| 400 | The group reservation status is `Submitted`. | `{"message": "Group reservation was submitted.", "errorCode": "GroupReservationSubmitted"}` |
| 400 | The group reservation status is `Closed`. | `{"message": "Group reservation is expired.", "errorCode": "GroupReservationExpired"}` |
| 404 | The guest session does not exist, or it belongs to a different group reservation. | `{"error": "Guest session with id {guestSessionId} not found."}` |
| 400 | The session is the owner session of the group. | `{"message": "The owner session cannot be deleted.", "errorCode": "OwnerSessionNotDeletable"}` |
| 500 | Another error occurs, for example a DynamoDB error. | `{"error": "Internal Server Error"}` |

The checks run in the order of the table. In the messages, `{guestSessionId}` is the value from the request.

Notes:
- This request works only when the group reservation status is `Open`.
- The owner cannot delete the owner session.
- The owner can delete a guest session with `isSubmitted: true`. A later group submit does not include the dishes of that session.
- `participants` lists only guest sessions. The owner is in `owner`.
- If a dish no longer exists, the response leaves out the order items for that dish. The totals do not include them.
- A second DELETE of the same `guestSessionId` returns 404. The client can treat this 404 as "already deleted".
- If the token is missing or not valid, the API Gateway Cognito authorizer sends 401 before the Lambda runs. That body comes from API Gateway, not from the table above.