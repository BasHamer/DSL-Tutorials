Feature: ServiceNow
	https://developer.servicenow.com/app.do#!/instance

@mytag
Scenario: Add two numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen

#
#	filter Incident
#	click Create New
#	Caller Abel Tuter
#	short description sometext