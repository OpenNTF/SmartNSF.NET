# SmartNSF.NET

.NET Proxy for SmartNSF Backends

## Purpose

.SmartNSF.Net provieds you a Proxy Service for .NET Core 6.0 Applications like Azure Functions. The main purpose is to encapsulate the call to the SmartNSF Backend as a Proxy. There are different authentication strategies how you can access SmartNSF Endpoints. SmartNSF.Net will provide you some helpfer classes to call the endpoints based on your choosen strategy.

### Accessing the SmartNSF Endpoint with a defined header value as key

You have defined on your SmartNSF Endpoint a route that looks like this:

```groovy
router.GET('api/products', {
	strategy (DOCUMENTS_BY_VIEW) {
		databaseName(db_path)
		viewName('luProductsID')
		runAsSigner(true)
	}
	allowedAccess { context->
		def key = context.getRequest().getHeader('smartnsf-api-key');
		return key == 'my-famouse-api-key';
	}
	mapJson 'UNID', json:'unid', type:'STRING', isformula:true, formula:'@Text(@DocumentUniqueID)'
	mapJson "ID", json:'id', type:'STRING'
	mapJson 'Sort', json:'sort', type:'DOUBLE'
	mapJson "Placeholder", json:'placeHolder', type:'STRING'
	mapJson "Value", json:'title', type:'STRING'
	mapJson "ImageFileName", json:'imageFileName', type:'STRING', isformula:true, formula:'@AttachmentNames'
})
```

allowedAccess expects a Header in your request called "smartnsf-api-key" (you can name the header as you want, but do not use authorization, because the domino http task will react on this header!). As you may notice, the complete call will be executed as signer (runAsSigner(true)).

## OPENNTF

    This project is an OpenNTF project, and is available under the Apache License, V2.0.
    All other aspects of the project, including contributions,  defect reports, discussions,
    feature requests and reviews are subject to the OpenNTF Terms of Use - available at
    http://openntf.org/Internal/home.nsf/dx/Terms_of_Use .
