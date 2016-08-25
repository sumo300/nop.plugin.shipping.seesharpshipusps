# SeeSharpShip USPS Plug-In for nopCommerce

This plug-in is a re-implementation of the USPS shipping plug-in for nopCommerce using the [SeeSharpShip](http://www.seesharpship.com/) shipping library.

## Release Notes

### v2.0.4 
* Updated to support nopCommerce v3.80

## Installation in nopCommerce

1. [Download a pre-built version](https://bitbucket.org/Sumo/nop.plugin.shipping.seesharpshipusps/downloads) for your version of nopCommerce.
2. Set aside your settings for the plugin (necessary until there is an in-place upgrade feature).
3. Uninstall current version if there is already one installed from nopCommerce's admin.
4. Copy the Shipping.SeeSharpShipUsps folder to your nopCommerce Plugins folder.
5. Install the new version from nopCommerce's admin.
6. Reconfigure module.

## Contribution guidelines

* Follow [nopCommerce Developer Guidelines](http://docs.nopcommerce.com/display/nc/Developer+Guide)
* Pull requests accepted
* Report [issues](https://bitbucket.org/Sumo/nop.plugin.shipping.seesharpshipusps/issues?status=new&status=open) via the [Issues](https://bitbucket.org/Sumo/nop.plugin.shipping.seesharpshipusps/issues?status=new&status=open) tab

## Additional Notes

* 'Copy local' property of the referenced assemblies are set to 'false'. We know that they're referenced by the main web applications. So there's no need to deploy them.  It can dramatically reduce package size.
* Set project output path to `..\..\Presentation\Nop.Web\Plugins\{PluginName}\` (both `Release` and `Debug` configurations).
* All views should marked as embedded resources.