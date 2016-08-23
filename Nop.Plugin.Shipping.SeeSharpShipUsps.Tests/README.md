# Testing With USPS API

To be able to run any of the unit tests here, you'll need an account with USPS.

## Register With USPS

You will need to [register](https://registration.shippingapis.com/) for the USPS Web Tools to get a testing account.

## Setup

Once registered, you'll receive an email with your Username and Password.  Getting
rates does not require the password, but keep it in a safe place.  This email will also
contain the production URL to the API, an 
[API user guide](https://www.usps.com/business/web-tools-apis/general-api-developer-guide.pdf),
and a link to the 
[tools Web site](https://www.usps.com/business/web-tools-apis/technical-documentation.htm).

* Production API URL
`http://production.shippingapis.com/ShippingAPI.dll`

Open the App.config file and update your credentials and the API URL if it has
changed since this document was written.  The test will use this configuration when run.
