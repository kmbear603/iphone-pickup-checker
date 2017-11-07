# iPhonePickupChecker

Apple provides a webpage to check for pickup availability of iPhones at nearby Apple stores. However it is very often to be out of stock as initial stock is too limited. iPhonePickupChecker is a tool to keep track of pickup availability and send email notification when there is any change.

The work of iPhonePickupChecker is based on the followings:
- zip code
- carrier
- iPhone model

## Source of availability data
iPhonePickupChecker makes use of the following API on apple.com for checking availability:

    https://www.apple.com/shop/retail/pickup-message?pl=true&parts.0={PART-NUMBER-1}&parts.1={PART-NUMBER-2}&parts.2={PART-NUMBER-3}&cppart={CARRIER-ID}&location={ZIP-CODE}

For example:

    https://www.apple.com/shop/retail/pickup-message?pl=true&parts.0=MQAK2LL/A&parts.1=MQAN2LL/A&cppart=ATT/US&location=27707

## About the code
- operating system: Microsoft Windows
- build environment: Visual Studio 2015
- programming language: C#
- external dependencies:
    - Newtonsoft.Json

## How to run
1. put config.json at the same directory as iPhonePickupChecker.exe. See the section "Configuration" for more detail about config.json
1. run iPhonePickupChecker.exe

## Configuration (config.json)
Here is a sample of config.json.

    {
        "zip-code": 27707,
        "notification": {
            "sender": {
                "email": "sender@gmail.com",
                "password": "sender gmail app password"
            },
            "recipients": [
                "receiver@gmail.com"
            ]
        },
        "carriers": [
            {
                "id": "ATT/US",
                "name": "AT&T",
                "selected": true
            },
            {
                "id": "TMOBILE/US",
                "name": "T-Mobile",
                "selected": true
            },
            {
                "id": "SPRINT/US",
                "name": "Sprint",
                "selected": true
            },
            {
                "id": "VERIZON/US",
                "name": "Verizon",
                "selected": true
            }
        ],
        "products": [
            {
                "id": "MQAK2LL/A",
                "name": "iPhone X Silver 64G",
                "selected": true
            },
            {
                "id": "MQAN2LL/A",
                "name": "iPhone X Silver 256G",
                "selected": true
            },
            {
                "id": "MQAJ2LL/A",
                "name": "iPhone X Space Gray 64G",
                "selected": true
            },
            {
                "id": "MQAM2LL/A",
                "name": "iPhone X Space Gray 256G",
                "selected": true
            }
        ],
        "reserve-url": "https://www.apple.com/shop/buy-iphone/iphone-x"
    }

## Author
kmbear603@gmail.com
