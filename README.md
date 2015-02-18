# Umbraco Load Balance Tester

Simple web app to test umbraco load balanced setup

This is an ASP.Net 5 project, which currently works with KRE version: 1.0.0-beta3-11030 CoreCLR

Use "k web" to run

The server addresses are hard coded in: `StatusController` which you'll need to update

On the Umbraco site(s), you need to create a doc type at the root called LBTestContainer, it needs to be a list view and must allow another doc type called: LBTestDoc to be allowed as children.

You then need to update `LBTestingKitController.CreateContent` method with your custom root node Id (committed as 5370)
