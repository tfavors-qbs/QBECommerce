# API Authorization Summary

This document outlines the authorization requirements for all API endpoints in the application.

## ?? Authorization Overview

### ? Public Endpoints (No Authentication Required)

#### AccountsController (`/api/accounts`)
- `POST /register` - User registration
- `POST /login` - Standard login
- `POST /login/ariba` - Ariba punch-out login
- `GET /info` - **[Authorize]** - Get current user info

#### PunchOutSessionsController (`/api/punchoutsessions`)
- `POST /request-punch-out` - **[AllowAnonymous]** - Ariba cXML integration endpoint
- `GET /sessionid/{sessionId}` - **[AllowAnonymous]** - Session lookup for login flow

#### Product Catalog Endpoints (Read-Only, Public for browsing)
All GET endpoints are public to allow catalog browsing:
- `GET /api/materials` - List all materials
- `GET /api/materials/{id}` - Get specific material
- `GET /api/classes` - List all classes
- `GET /api/classes/{id}` - Get specific class
- `GET /api/coatings` - List all coatings
- `GET /api/coatings/{id}` - Get specific coating
- `GET /api/diameters` - List all diameters
- `GET /api/diameters/{id}` - Get specific diameter
- `GET /api/lengths` - List all lengths
- `GET /api/lengths/{id}` - Get specific length
- `GET /api/shapes` - List all shapes
- `GET /api/shapes/{id}` - Get specific shape
- `GET /api/specs` - List all specs
- `GET /api/specs/{id}` - Get specific spec
- `GET /api/threads` - List all threads
- `GET /api/threads/{id}` - Get specific thread
- `GET /api/groups` - List all groups
- `GET /api/groups/{id}` - Get specific group
- `GET /api/productids` - List all product IDs
- `GET /api/productids/{id}` - Get specific product ID
- `GET /api/skus` - List all SKUs
- `GET /api/skus/{id}` - Get specific SKU

---

### ?? Authenticated Endpoints (Requires Login)

#### ContractItemsApiController (`/api/contractitems`)
- `GET /` - **[Authorize]** - List contract items (filtered by user's client)
- `GET /{id}` - **[Authorize]** - Get specific contract item (with client verification)

#### ShoppingCartsAPIController (`/api/shoppingcarts`)
**[Authorize]** on controller level - All endpoints require authentication:
- `GET /{id}` - Get shopping cart by ID
- `GET /` - Get current user's shopping cart
- `GET /get-cart-info` - Get cart page info
- `POST /add-item` - Add item to cart
- `PUT /items/{id}` - Update cart item
- `DELETE /items/{id}` - Delete cart item
- `DELETE /clear/{cartId}` - Clear shopping cart

#### ShoppingCartItemsAPIController (`/api/shoppingcartitems`)
**[Authorize]** on controller level - All endpoints require authentication:
- `GET /shoppingcart/{cartId}` - Get cart items by shopping cart ID

---

### ?? Admin-Only Endpoints (Requires Admin Role)

#### ClientsApiController (`/api/clients`)
**[Authorize(Roles = "Admin")]** on all endpoints:
- `GET /` - List all clients
- `GET /{id}` - Get specific client
- `POST /` - Create new client
- `PUT /{id}` - Update client
- `DELETE /{id}` - Delete client

#### ContractItemsApiController (`/api/contractitems`)
**[Authorize(Roles = "Admin")]** for modifications:
- `POST /` - Create contract item
- `POST /range` - Bulk create contract items
- `PUT /{id}` - Update contract item
- `DELETE /{id}` - Delete contract item

#### PunchOutSessionsController (`/api/punchoutsessions`)
**[Authorize(Roles = "Admin")]** for management:
- `GET /` - List all sessions
- `GET /{id}` - Get specific session
- `POST /` - Create session
- `PUT /{id}` - Update session
- `DELETE /{id}` - Delete session

#### Product Catalog Management (All modification operations)
**[Authorize(Roles = "Admin")]** for all POST, PUT, DELETE operations:

**MaterialsApiController** (`/api/materials`)
- `POST /` - Create material
- `POST /range` - Bulk create materials
- `PUT /{id}` - Update material
- `DELETE /{id}` - Delete material

**ClassesApiController** (`/api/classes`)
- `POST /` - Create class
- `POST /range` - Bulk create classes
- `PUT /{id}` - Update class
- `DELETE /{id}` - Delete class

**CoatingsApiController** (`/api/coatings`)
- `POST /` - Create coating
- `POST /range` - Bulk create coatings
- `PUT /{id}` - Update coating
- `DELETE /{id}` - Delete coating

**DiametersApiController** (`/api/diameters`)
- `POST /` - Create diameter
- `POST /range` - Bulk create diameters
- `PUT /{id}` - Update diameter
- `DELETE /{id}` - Delete diameter

**LengthsApiController** (`/api/lengths`)
- `POST /` - Create length
- `POST /range` - Bulk create lengths
- `PUT /{id}` - Update length
- `DELETE /{id}` - Delete length

**ShapesApiController** (`/api/shapes`)
- `POST /` - Create shape
- `POST /range` - Bulk create shapes
- `PUT /{id}` - Update shape
- `DELETE /{id}` - Delete shape

**SpecsApiController** (`/api/specs`)
- `POST /` - Create spec
- `POST /range` - Bulk create specs
- `PUT /{id}` - Update spec
- `DELETE /{id}` - Delete spec

**ThreadsApiController** (`/api/threads`)
- `POST /` - Create thread
- `POST /range` - Bulk create threads
- `PUT /{id}` - Update thread
- `DELETE /{id}` - Delete thread

**GroupsApiController** (`/api/groups`)
- `POST /` - Create group
- `POST /range` - Bulk create groups
- `PUT /{id}` - Update group
- `DELETE /{id}` - Delete group

**ProductIDsApiController** (`/api/productids`)
- `POST /` - Create product ID
- `POST /range` - Bulk create product IDs
- `PUT /{id}` - Update product ID
- `DELETE /{id}` - Delete product ID

**SKUsApiController** (`/api/skus`)
- `POST /` - Create SKU
- `POST /range` - Bulk create SKUs
- `PUT /{id}` - Update SKU
- `DELETE /{id}` - Delete SKU

---

## ?? Authorization Strategy Summary

### Public Access
- **Login/Registration** - Must be public for user onboarding
- **Ariba Integration** - External system integration requires public endpoints
- **Product Catalog Browsing** - Public read access allows users to browse before login

### Authenticated Access
- **Shopping Cart Operations** - Users must be logged in to manage their cart
- **Contract Items** - Authenticated users can view their client's contract items
- **User Info** - Users can access their own profile information

### Admin Access
- **Client Management** - Sensitive business data, admin only
- **Catalog Management** - Product data modifications require admin privileges
- **Contract Item Management** - Creating/modifying pricing requires admin
- **PunchOut Session Management** - System management requires admin

### Security Features
- ? Client-based data isolation for contract items
- ? User can only access their own shopping cart
- ? Admin role required for all data modifications
- ? Public endpoints explicitly marked with `[AllowAnonymous]`
- ? Authenticated endpoints use `[Authorize]` attribute
- ? Role-based endpoints use `[Authorize(Roles = "Admin")]`

---

## ?? Notes

1. **Empty IdentityController** - This controller exists but has no endpoints defined yet.

2. **Role Management** - The application needs to ensure the "Admin" role is properly configured and assigned to administrative users.

3. **JWT vs Cookie Authentication** - The application supports both authentication methods. Ensure both are properly configured in the middleware pipeline.

4. **Client Verification** - Contract items GET by ID includes client verification to prevent unauthorized access to other clients' data.

---

## ?? Recommendations

1. **Consider Rate Limiting** - Public endpoints (especially login/register) should have rate limiting to prevent abuse.

2. **Add Logging** - Consider adding audit logging for all Admin operations.

3. **API Documentation** - Consider adding Swagger authorization requirements documentation.

4. **CORS Configuration** - Ensure CORS is properly configured for the client applications that will consume these APIs.

5. **Session Management** - Review the PunchOut session expiration and cleanup strategy.
