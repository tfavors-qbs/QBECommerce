# Debugging cXML Message Sent to Ariba

## Enhanced Logging Added

To help diagnose why items aren't appearing in Ariba's cart, I've added comprehensive logging to show **exactly** what's being sent.

## What You'll See in Console

### 1. Cart Items Details
```
[HH:mm:ss] Cart items details:
  - Item #123: BOLT-HEX-1/2-13X2
    Description: 1/2"-13 x 2" Hex Bolt Grade 5
    Quantity: 10
    Price: $0.50
    Total: $5.00
  - Item #124: NUT-HEX-1/2-13
    Description: 1/2"-13 Hex Nut Grade 5
    Quantity: 10
    Price: $0.25
    Total: $2.50
```

### 2. Full cXML Message
```
?? FULL cXML ORDER MESSAGE
  <?xml version="1.0" encoding="utf-8"?>
  <cXML>
    <Header>
      <From>
        <Credential domain="NetworkId">
          <Identity>AN01000625964</Identity>
        </Credential>
      </From>
      <To>
        <Credential domain="DUNS">
          <Identity>123456789</Identity>
        </Credential>
      </To>
      <Sender>
        <Credential domain="shop.qualitybolt.com">
          <Identity>shop.qualitybolt.com</Identity>
        </Credential>
        <UserAgent>Quality Bolt & Screw Punch Out Application</UserAgent>
      </Sender>
    </Header>
    <Message>
      <PunchOutOrderMessage>
        <BuyerCookie>EfndEX6EMe2cq32g48p347R1yXKUdNVW0.412280158081003532</BuyerCookie>
        <PunchOutOrderMessageHeader operationAllowed="edit">
          <Total>
            <Money currency="USD">7.50</Money>
          </Total>
        </PunchOutOrderMessageHeader>
        <ItemIn quantity="10">
          <ItemID>
            <SupplierPartID>BOLT-HEX-1/2-13X2</SupplierPartID>
            <SupplierPartAuxiliaryID>123</SupplierPartAuxiliaryID>
          </ItemID>
          <ItemDetail>
            <UnitPrice>
              <Money currency="USD">5.00</Money>
            </UnitPrice>
            <Description xml:lang="en">1/2"-13 x 2" Hex Bolt Grade 5</Description>
            <UnitOfMeasure>EA</UnitOfMeasure>
            <Classification domain="UNSPSC">31160000</Classification>
          </ItemDetail>
        </ItemIn>
        <!-- More items... -->
      </PunchOutOrderMessage>
    </Message>
  </cXML>
```

## How to Use This

### Step 1: Test Checkout
1. Open browser console (F12)
2. Add items to cart
3. Click "Check Out"
4. Watch console output

### Step 2: Examine the cXML

Look for the **?? FULL cXML ORDER MESSAGE** section in console. This shows the exact XML being sent to Ariba.

**Check these key elements:**

#### BuyerCookie
```xml
<BuyerCookie>EfndEX6EMe2cq32g48p347R1yXKUdNVW0...</BuyerCookie>
```
? Must match the session cookie from Ariba  
? If wrong, Ariba won't recognize the session

#### Total Amount
```xml
<Total>
  <Money currency="USD">7.50</Money>
</Total>
```
? Should be sum of all items  
? If wrong, validation might fail

#### Each Item
```xml
<ItemIn quantity="10">
  <ItemID>
    <SupplierPartID>BOLT-HEX-1/2-13X2</SupplierPartID>
    <SupplierPartAuxiliaryID>123</SupplierPartAuxiliaryID>
  </ItemID>
  <ItemDetail>
    <UnitPrice>
      <Money currency="USD">5.00</Money>
    </UnitPrice>
    <Description xml:lang="en">...</Description>
    <UnitOfMeasure>EA</UnitOfMeasure>
  </ItemDetail>
</ItemIn>
```

**Key Fields:**
- `quantity`: Number of items
- `SupplierPartID`: SKU/Part number
- `SupplierPartAuxiliaryID`: Internal item ID
- `UnitPrice`: Price for this quantity
- `Description`: Item description
- `UnitOfMeasure`: "EA" for each

### Step 3: Common Issues to Look For

#### Issue 1: Wrong BuyerCookie
**Symptom:** Ariba returns 200 OK but doesn't add items

**Check:**
```
Console: BuyerCookie: EfndEX6EMe2c...
Session: BuyerCookie: EfndEX6EMe2c...
```
Must match exactly!

#### Issue 2: Missing or Wrong ItemID
**Symptom:** Items don't appear in cart

**Check:**
- `SupplierPartID` should be the SKU name
- `SupplierPartAuxiliaryID` should be the contract item ID
- Both must exist in your catalog

#### Issue 3: Price Mismatch
**Symptom:** Ariba shows error or different price

**Check:**
- `UnitPrice` should be `quantity * unit_price`
- `Total` should be sum of all `UnitPrice` values
- Currency should be "USD"

#### Issue 4: Invalid cXML Format
**Symptom:** Ariba returns error response

**Check:**
- XML is well-formed (no syntax errors)
- All required fields are present
- Namespace declarations if needed

### Step 4: Validate cXML

Copy the full cXML from console and:

1. **Check XML Syntax:**
   - Paste into XML validator online
   - Look for syntax errors

2. **Check Against Ariba Schema:**
   - Compare with Ariba's cXML documentation
   - Ensure all required fields present

3. **Test in Ariba's Catalog Tester:**
   - If available, paste cXML into tester
   - See if Ariba accepts it

## Expected vs Actual

### Expected Behavior
```
1. POST cXML to Ariba
2. Ariba returns 200 OK
3. Ariba parses cXML
4. Ariba adds items to cart
5. Ariba closes iframe
6. User sees items in cart ?
```

### If Items Don't Appear
```
1. POST cXML to Ariba ?
2. Ariba returns 200 OK ?
3. Ariba parses cXML ?
4. Problem here ? cXML issue
```

**Possible cXML Issues:**
- Wrong BuyerCookie
- Invalid Item IDs
- Price calculation wrong
- Missing required fields
- Wrong XML format

## Troubleshooting Checklist

After checking out, verify:

- [ ] Console shows cart items details
- [ ] Console shows full cXML message
- [ ] BuyerCookie matches session
- [ ] All items have SupplierPartID
- [ ] Quantities are correct
- [ ] Prices are correct
- [ ] Total matches sum
- [ ] No XML syntax errors
- [ ] POST returns 200 OK
- [ ] Response content is examined

## What to Send Me

If items still don't appear, copy these from console:

1. **Cart Items Details** (the logged items)
2. **Full cXML Message** (the complete XML)
3. **POST Response** (status code and response content)
4. **Any Error Messages** from Ariba

With this information, we can diagnose:
- If cXML format is wrong
- If item IDs don't match
- If prices are incorrect
- If session is invalid

---

**Status**: ? Enhanced logging added  
**Build**: ? Successful  
**Next**: Test and examine console output  

**Deploy and test - the console will show exactly what's being sent to Ariba!** ??
