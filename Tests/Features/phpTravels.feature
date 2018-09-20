@SingleBrowser
Feature: phpTravels
https://phptravels.com/demo/

Scenario: Find flights
	Given navigated to 'https://www.phptravels.net/'
	When entering 'Denv' into element 'hotel_s2_text'
	And Clicking the element 'Denver, United States'
	And selecting the element 'Check in' 
	And clicking the element 'Today.Day'
	And clicking the element 'Tomorrow.Day'
	And clicking the element'Search'
	Then the page contains 'No Results Found'


Scenario: hotel details
	Given navigated to 'https://www.phptravels.net/hotels/listing/'
	When clicking the element 'Details' under 'Hyatt Regency Perth'
	Then the page contains '$300' in row 'EXECUTIVE TWO-BEDROOMS APARTMENT'


