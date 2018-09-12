Feature: Variables

Scenario: Create an Employee
	Given the Employee
	| var | Name |
	| E1  | Bob  |

Scenario: Validate a Name
	Given the Employee
	| var | Name |
	| E1  | Bob  |
	Then 'E1.Name' has the value 'Bob'

Scenario: Heirarchy blank
	Given the Employee
	| var | Name | Reports |
	| E1  | Bob  |         |

Scenario: Heirarchy null
	Given the Employees
	| var | Name | Reports |
	| E1  | Bob  | null    |

Scenario: Heirarchy single
	Given the Employee
	| var | Name |
	| E1  | Bob  |
	Given the Employee
	| var | Name | Reports |
	| E2  | Mary | E1      |
	Then 'E2.Reports' contains the values
	| Name    |
	| E1.Name |

Scenario: Heirarchy multi
	Given the Employees
	| var | Name | 
	| E1  | Bob  | 
	| E2  | Bob2 | 
	Given the Employee
	| var | Name | Reports |
	| E3  | Mary | E1, E2  |
	Then 'E3.Reports' contains the values
	| Name    |
	| E1.Name |
	| E2.Name |
