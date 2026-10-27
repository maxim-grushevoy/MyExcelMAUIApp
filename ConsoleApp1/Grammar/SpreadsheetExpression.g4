grammar SpreadsheetExpression;

options { caseInsensitive = true; }

/*
 * Варіант 4: операції та функції 1, 2, 3, 7
 *   1) + - * /  (бінарні)
 *   2) mod, div
 *   3) + -      (унарні)
 *   7) mmax(x1,...,xN), mmin(x1,...,xN), N >= 1
 * Операнди: цілі числа довільної довжини, імена клітинок (A3), круглі дужки.
 */

/*
 * Parser Rules
 */

compileUnit : ASSIGN? expression EOF ;

// порядок альтернатив задає пріоритет (від вищого до нижчого)
expression
    : LPAREN expression RPAREN                              #ParenExpr
    | func=(MMAX | MMIN) LPAREN expression (COMMA expression)* RPAREN #FuncExpr
    | op=(PLUS | MINUS) expression                          #UnaryExpr
    | expression op=(MUL | DIV | IDIV | MOD) expression     #MulExpr
    | expression op=(PLUS | MINUS) expression               #AddExpr
    | NUMBER                                                #NumberExpr
    | CELL                                                  #CellExpr
    ;

/*
 * Lexer Rules
 */

MMAX   : 'mmax' ;
MMIN   : 'mmin' ;
IDIV   : 'div' ;
MOD    : 'mod' ;

PLUS   : '+' ;
MINUS  : '-' ;
MUL    : '*' ;
DIV    : '/' ;
ASSIGN : '=' ;
LPAREN : '(' ;
RPAREN : ')' ;
COMMA  : ',' ;

NUMBER : [0-9]+ ;
CELL   : [a-z]+ [1-9] [0-9]* ;

WS : [ \t\r\n]+ -> channel(HIDDEN) ;
