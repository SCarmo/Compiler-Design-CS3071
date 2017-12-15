    LDR     R5, =1024
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 STR R5, [R2] ; limit
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 STR R6, [R2] ; test
; Procedure Test
TestBody
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 LDR R7, [R2] ; limit
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    STR     R7, [R2]        ; j
    LDR     R6, =1
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    LDR     R5, [R2]        ; j
    ADD     R5, R5, R6
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; j
    LDR     R8, =1
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    LDR     R7, [R2]        ; j
    SUB     R7, R7, R8
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    STR     R7, [R2]        ; j
    LDR     R9, =6
    ADD     R2, BP, #16
    LDR     R1, =4
    ADD     R2, R2, R1, LSL #2
    STR     R9, [R2]        ; age
    LDR     R5, =21
    ADD     R2, BP, #16
    LDR     R1, =7
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; age
    LDR     R5, =10
    ADD     R2, BP, #16
    LDR     R1, =4
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; age
    LDR     R5, =20
    ADD     R2, BP, #16
    LDR     R1, =5
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; height
    LDR     R5, =0
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; j
L1
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    LDR     R5, [R2]        ; j
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 LDR R6, [R2] ; limit
    CMP     R5, R6
    MOVLT   R5, #1
    MOVGE   R5, #0
    MOVS    R5, R5          ; reset Z flag in CPSR
    BEQ     L2              ; jump on condition false
    LDR     R6, =1
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    LDR     R5, [R2]        ; j
    ADD     R5, R5, R6
    ADD     R2, BP, #16
    LDR     R1, =0
    ADD     R2, R2, R1, LSL #2
    STR     R5, [R2]        ; j
    LDR     R8, =1
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 LDR R7, [R2] ; i
    ADD     R7, R7, R8
 LDR R2, =0
 ADD R2, R4, R2, LSL #2
 STR R7, [R2] ; i
    B       L1
L2
    MOV     TOP, BP         ; reset top of stack
    LDR     BP, [TOP,#12]   ; and stack base pointers
    LDR     PC, [TOP]       ; return from Test
Test
    LDR     R0, =1          ; current lexic level
    LDR     R1, =10          ; number of local variables
    BL      enter           ; build new stack frame
    B       TestBody
; j is a local variable of type Integer 
; sum is a local variable of type Integer 
; g is a local variable of type Integer 
; k is a local variable of type Boolean 
constant 
; mytestage is a local variable of type Integer 
; mytestheight is a local variable of type Integer 
; mytestfresh is a local variable of type Boolean 
constant 
; anotherTestage is a local variable of type Integer 
; anotherTestheight is a local variable of type Integer 
; anotherTestfresh is a local variable of type Boolean 
; limit is a scope 
; i is a global variable of type Integer 
constant 
; age is a global variable of type Integer 
; height is a global variable of type Integer 
; fresh is a global variable of type Boolean 
; Test is a procedure 
MainBody
    ADD     R0, PC, #4      ; store return address
    STR     R0, [TOP]       ; in new stack frame
    B       Test
StopTest
    B       StopTest
Main
    LDR     R0, =1          ; current lexic level
    LDR     R1, =0          ; number of local variables
    BL      enter           ; build new stack frame
    B       MainBody
; limit is a scope 
; i is a global variable of type Integer 
constant 
; age is a global variable of type Integer 
; height is a global variable of type Integer 
; fresh is a global variable of type Boolean 
; Test is a procedure 
; main is a procedure 
; limit is a scope 
; i is a global variable of type Integer 
constant 
; age is a global variable of type Integer 
; height is a global variable of type Integer 
; fresh is a global variable of type Boolean 
; Test is a procedure 
; main is a procedure 
