CoDeSys+$   �                   @        @   2.3.9.59    @/    @                             �=�c +    @      .Q����sb             @>�c        h   @   q   C:\TwinCAT\PLC\LIB\STANDARD.LIB @                                                                                          CONCAT               STR1               ��              STR2               ��                 CONCAT                                         ��66  �   ����           CTD           M             ��           Variable for CD Edge Detection      CD            ��           Count Down on rising edge    LOAD            ��           Load Start Value    PV           ��           Start Value       Q            ��           Counter reached 0    CV           ��           Current Counter Value             ��66  �   ����           CTU           M             ��            Variable for CU Edge Detection       CU            ��       
    Count Up    RESET            ��           Reset Counter to 0    PV           ��           Counter Limit       Q            ��           Counter reached the Limit    CV           ��           Current Counter Value             ��66  �   ����           CTUD           MU             ��            Variable for CU Edge Detection    MD             ��            Variable for CD Edge Detection       CU            ��	       
    Count Up    CD            ��
           Count Down    RESET            ��           Reset Counter to Null    LOAD            ��           Load Start Value    PV           ��           Start Value / Counter Limit       QU            ��           Counter reached Limit    QD            ��           Counter reached Null    CV           ��           Current Counter Value             ��66  �   ����           DELETE               STR               ��              LEN           ��              POS           ��                 DELETE                                         ��66  �   ����           F_TRIG           M             ��
                 CLK            ��           Signal to detect       Q            ��           Edge detected             ��66  �   ����           FIND               STR1               ��              STR2               ��                 FIND                                     ��66  �   ����           INSERT               STR1               ��              STR2               ��              POS           ��                 INSERT                                         ��66  �   ����           LEFT               STR               ��              SIZE           ��                 LEFT                                         ��66  �   ����           LEN               STR               ��                 LEN                                     ��66  �   ����           MID               STR               ��              LEN           ��              POS           ��                 MID                                         ��66  �   ����           R_TRIG           M             ��
                 CLK            ��           Signal to detect       Q            ��           Edge detected             ��66  �   ����           REPLACE               STR1               ��              STR2               ��              L           ��              P           ��                 REPLACE                                         ��66  �   ����           RIGHT               STR               ��              SIZE           ��                 RIGHT                                         ��66  �   ����           RS               SET            ��              RESET1            ��                 Q1            ��
                       ��66  �   ����           SEMA           X             ��                 CLAIM            ��	              RELEASE            ��
                 BUSY            ��                       ��66  �   ����           SR               SET1            ��              RESET            ��                 Q1            ��	                       ��66  �   ����           TOF           M             ��           internal variable 	   StartTime            ��           internal variable       IN            ��       ?    starts timer with falling edge, resets timer with rising edge    PT           ��           time to pass, before Q is set       Q            ��	       2    is FALSE, PT seconds after IN had a falling edge    ET           ��
           elapsed time             ��66  �   ����           TON           M             ��           internal variable 	   StartTime            ��           internal variable       IN            ��       ?    starts timer with rising edge, resets timer with falling edge    PT           ��           time to pass, before Q is set       Q            ��	       0    is TRUE, PT seconds after IN had a rising edge    ET           ��
           elapsed time             ��66  �   ����           TP        	   StartTime            ��           internal variable       IN            ��       !    Trigger for Start of the Signal    PT           ��       '    The length of the High-Signal in 10ms       Q            ��	           The pulse    ET           ��
       &    The current phase of the High-Signal             ��66  �   ����    R    @                                                                                          MAIN                             �E�c  @    ����            
 �    !   "          ( W      K   e     K   s     K   �     K   �                 �         +     ��localhost       � � xD2�D2 � sƨwJ           ���@ � p�w�)R������    ��  �D+     ��@  2        �         ��         �         ��	�U�� �	�� ¨w        D+ )2D+ X�   D+ ��@      D+ ��@ <�   <� � d�� �d����� ;�     ,   ,                                                        K         @   �E�cx  /*BECKCONFI3*/
        !#;
 @   @   �   �     3               d   Standard            	Ձ�c     Y���yn3b           VAR_GLOBAL
END_VAR
                                                                                  "   , 3�> ^             Standardd         MAIN����               �=�c                 $����  C2����                 ����             Standard @>�c	@>�c                                      	�E�c     ;*����           VAR_CONFIG
END_VAR
                                                                                   '              , �  ;�           Global_Variables �E�c	Ձ�c      r d c         �  VAR_GLOBAL
(* Simple types *)
	AString			: STRING;
	ABool			: BOOL;
	AByte			: BYTE;
	AInt				: INT;
	ADInt			: DINT;
	(*ALInt			: LINT;*)
	AReal			: REAL;
	ALReal			: LREAL;
(* Arrays *)
	ArrString			: ARRAY [0..10] OF STRING;
	ArrBool			: ARRAY [0..10] OF BOOL;
	ArrByte			: ARRAY [0..10] OF BYTE;
	ArrInt			: ARRAY [0..10] OF INT;
	ArrDInt			: ARRAY [0..10] OF DINT;
	(*ArrLInt			: ARRAY [0..10] OF LINT;*)
	ArrReal			: ARRAY [0..10] OF REAL;
	ArrLReal			: ARRAY [0..10] OF LREAL;
(* Structures *)
	Tag1			: STRUCT_Object;
	Tag2			: STRUCT_Object2;
	Tag3			: STRUCT_Object3;
(* Hardware Mappings *)
	Out1				AT %QX0.0	: BOOL;
	Out2				AT %QX0.1	: BOOL;
	Out3				AT %QX0.2	: BOOL;
	Out4				AT %QX0.3	: BOOL;
END_VAR
                                                                                               '           	                        Variable_Configuration �E�c	�E�c	                        VAR_CONFIG
END_VAR
                                                                                                 �   |0|0 @|    @Z   MS Sans Serif @       HH':'mm':'ss @      dd'-'MM'-'yyyy   dd'-'MM'-'yyyy HH':'mm':'ss�����                               4     �   ���  �3 ���   � ���     
    @��  ���     @      DEFAULT             System      �   |0|0 @|    @Z   MS Sans Serif @       HH':'mm':'ss @      dd'-'MM'-'yyyy   dd'-'MM'-'yyyy HH':'mm':'ss�����                      )   HH':'mm':'ss @                             dd'-'MM'-'yyyy @       '   !   , i  ��           STRUCT_Object ��c	��c      )Artrg	          TYPE STRUCT_Object :
STRUCT
(* Simple types *)
	AString			: STRING;
	ABool			: BOOL;
	AByte			: BYTE;
	AInt				: INT;
	ADInt			: DINT;
	(*ALInt			: LINT;*)
	AReal			: REAL;
	ALReal			: LREAL;
(* Structure in a structure *)
	Object2			: STRUCT_Object2;
END_STRUCT
END_TYPE             "   ,     i�           STRUCT_Object2 ��c	��c      RE;NDTR          TYPE STRUCT_Object2 :
STRUCT
(* Simple types *)
	AString			: STRING;
	ABool			: BOOL;
	AByte			: BYTE;
	AInt				: INT;
	ADInt			: DINT;
	(*ALInt			: LINT;*)
	AReal			: REAL;
	ALReal			: LREAL;
	(*Object3			: STRUCT_Object3;*)
END_STRUCT
END_TYPE             #   , C�� -�           STRUCT_Object3 �E�c	�E�c      		LRL;EN        q  TYPE STRUCT_Object3 :
STRUCT
(* Arrays *)
	ArrString			: ARRAY [0..10] OF STRING;
	ArrBool			: ARRAY [0..10] OF BOOL;
	ArrByte			: ARRAY [0..10] OF BYTE;
	ArrInt			: ARRAY [0..10] OF INT;
	ArrDInt			: ARRAY [0..10] OF DINT;
	(*ArrLInt			: ARRAY [0..10] OF LINT;*)
	ArrReal			: ARRAY [0..10] OF REAL;
	ArrLReal			: ARRAY [0..10] OF LREAL;
END_STRUCT
END_TYPE                  ,  � �           MAIN �=�c	�E�c                         PROGRAM MAIN
VAR
END_VAR�  ABool := AByte = 4;
IF (AByte < 7) THEN
AByte := AByte + 1;
ELSE
AByte := 0;
END_IF
IF (AInt < 100) THEN
AInt := AInt + 1;
ELSE
AInt := 0;
END_IF
IF (ADInt < 200) THEN
ADInt := ADInt + 1;
ELSE
ADInt := 100;
END_IF
IF (AReal < 20) THEN
AReal := AReal + 0.1;
ELSE
AReal := 0.0;
END_IF
IF (ALReal < 200) THEN
ALReal := ALReal + 1.1;
ELSE
ALReal := 0.0;
END_IF
AString := INT_TO_STRING(AInt);
                 ����  ��20˺            STANDARD.LIB @f�w5      CONCAT @                	   CTD @        	   CTU @        
   CTUD @           DELETE @           F_TRIG @        
   FIND @           INSERT @        
   LEFT @        	   LEN @        	   MID @           R_TRIG @           REPLACE @           RIGHT @           RS @        
   SEMA @           SR @        	   TOF @        	   TON @           TP @              Global Variables 0 @                          #�֛��N           2                ����������������  
             ����  psasinpr        ����  ���$ ��}                      POUs                MAIN      ����           
   Data types                 STRUCT_Object  !                   STRUCT_Object2  "                  STRUCT_Object3  #   ����             Visualizations  ����              Global Variables                 Global_Variables                     Variable_Configuration  	   ����                                                              @>�c                         	   localhost            P      	   localhost            P      	   localhost            P            �R��