<?php
foreach( $_GET as $variable => $valor ){ 
$_GET [ $variable ] = str_replace ( "+" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "%" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "\\" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "|" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "#" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "$" , "" , $_GET [ $variable ]);
$_GET [ $variable ] = str_replace ( "&" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( "'" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( "/" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( "*" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( "\"" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( ";" , "" , $_GET [ $variable ]); 
$_GET [ $variable ] = str_replace ( "=" , "" , $_GET [ $variable ]); 
} 
// Modificamos las variables de formularios 
foreach( $_POST as $variable => $valor ){ 
$_POST [ $variable ] = str_replace ( "+" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "%" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "\\" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "|" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "#" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "$" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "&" , "" , $_POST [ $variable ]); 
$_POST [ $variable ] = str_replace ( "'" , "" , $_POST [ $variable ]); 
$_POST [ $variable ] = str_replace ( "/" , "" , $_POST [ $variable ]); 
$_POST [ $variable ] = str_replace ( "*" , "" , $_POST [ $variable ]); 
$_POST [ $variable ] = str_replace ( "\"" , "" , $_POST [ $variable ]); 
$_POST [ $variable ] = str_replace ( ";" , "" , $_POST [ $variable ]);
$_POST [ $variable ] = str_replace ( "=" , "" , $_POST [ $variable ]); 
}

$register['cuenta'] = FALSE;
$register['contraseña'] = FALSE;
$register['email'] = FALSE;
$register['apodo'] = FALSE;
$register['pregunta'] = FALSE;
$register['respuesta'] = FALSE;

$cuenta = htmlentities(@$_POST['cuentaB']);
$contraseña = htmlentities(@$_POST['passB']);
$confir_contra = htmlentities(@$_POST['passB2']);
$email = htmlentities(@$_POST['emailB']);
$apellido = htmlentities(@$_POST['apellidoB']);
$nombre = htmlentities(@$_POST['nombreB']);

$idioma = htmlentities(@$_POST['langB']);
$pais = htmlentities(@$_POST['paisB']);
$cumpleañosdia = htmlentities(@$_POST['nacDiaB']);
$cumpleañosmes = htmlentities(@$_POST['nacMesB']);
$cumpleañosaño = htmlentities(@$_POST['nacAnoB']);
$ipregistro = htmlentities(@$_SERVER['REMOTE_ADDR']);
$apodo = htmlentities(@$_POST['apodoB']);
$pregunta = htmlentities(@$_POST['preguntaB']);
$respuesta = htmlentities(@$_POST['respuestaB']);

if ( $cuenta == '' ||$contraseña == '' ||$email == '' ||$apellido == '' ||$nombre == '' ||$idioma == '' ||$pais == '' ||$apodo == '' ||$pregunta == '' ||$respuesta == ''||$cumpleañosaño==''||$cumpleañosdia ==''||$cumpleañosmes =='')
{
	echo"&result=Campos Vacios";
	return;
}
else
{	
	$serv = new mysqli("127.0.0.1", "root", "", "bustar_cuentas");
	if (!$serv->connect_errno)
	{
		$serv->set_charset('utf8');
        if($ipregistro != '')
		{
			$sql = $serv->query("SELECT ipRegistro FROM cuentas WHERE ipRegistro = '".$ipregistro."'");
			if($sql->num_rows > 20)
			{
				echo "&result=No puedes crear mas de 20 cuentas con la misma IP";
				return;

			}
			$sql = $serv->query("SELECT ipRegistro FROM cuentas WHERE ultimaIP = '".$ipregistro."'");
			if($sql->num_rows > 20)
			{
				echo "&result=No puedes crear mas de 20 cuentas con la misma IP";
				return;
			}

		}
		else
		{
			echo "&result=IP ADDRESS EMPTY";
			return;
		}

		if($cuenta != '')
		{
			if(preg_match("/^[a-zA-Z0-9\-_.]+$/i",$cuenta))
			{
				$sql = $serv->query("SELECT * FROM cuentas WHERE cuenta = '".$cuenta."'");
				if($sql->num_rows < 1)
				{
					$register['cuenta'] = TRUE;
				}
				else
				{
					echo "&result=El nombre de esta cuenta ya existe, porfavor ingresa otra";
					return;
				}
			}
			else
			{
				echo "&result=El nombre de la cuenta no es valida, evita usar caracteres especiales";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa una cuenta";
			return;
		}

		if($contraseña != '' &&  $confir_contra != '')
		{
			if(preg_match("/^[a-zA-Z0-9\-_.]+$/i",$contraseña))
			{
				if($contraseña == $confir_contra)
				{
					$register['contraseña'] = TRUE;
				}
				else
				{
					echo "&result=Las contraseñas no son iguales, porfavor re-ingresa tu confirmacion de contraseña";
					return;
				}
			}
			else
			{
				echo "&result=La contraseña no es valida, evita usar caracteres especiales";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa una contraseña";
			return;
		}

		if($email != '')
		{	
			if(preg_match("/^[a-z0-9\-_.]+@[a-z0-9\-_.]+\.[a-z]{2,3}$/i",$email))
			{
				$register['email'] = TRUE;
			}
			else
			{
				echo "&result=Tu email no es valido, porfavor ingresa un email correcto";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa un email";
			return;
		}

		if( $pregunta != '')
		{
			if(preg_match("/^[a-zA-Z0-9\-_.\s?¿]+$/i",$pregunta))
			{
				$register['pregunta'] = TRUE;
			}
			else
			{
				echo "&result=Tu pregunta no es valida, evita usar caracteres especiales";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa una pregunta secreta";
			return;
		}

		if( $respuesta != '')
		{
			if(preg_match("/^[a-zA-Z0-9\-_.\s]+$/i",$respuesta))
			{
				$register['respuesta'] = TRUE;
			}
			else
			{
				echo "&result=Tu respuesta secreta no es valida, evita usar caracteres especiales";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa una respuesta secreta";
			return;
		}
		
		if($apodo != '')
		{
			if(preg_match("/^[a-zA-Z]+$/i",$apodo))
			{
			$sql = $serv->query("SELECT * FROM cuentas WHERE apodo = '".$apodo."'");
				if($sql->num_rows < 1)
				{
					$register['apodo'] = TRUE;
				}
				else
				{
					echo "&result=El apodo ingresado ya existe, porfavor inserta otro";
					return;
				}
			}
			else
			{
				echo "&result=Tu apodo no es valido, evita usar caracteres especiales";
				return;
			}
		}
		else
		{
			echo "&result=Porfavor ingresa un apodo";
			return;
		}

		if($register['cuenta'] && $register['contraseña'] && $register['email'] && $register['pregunta']&& $register['respuesta'] && $register['apodo'])
		{
			// a veces se tiene q agregar utf8_encode  o utf8_decode  segun corresponda en el dedicado y se soluciona el problema del mensaje de la /fx1
			$sentencia = ("INSERT INTO cuentas (cuenta,contraseña,nombre,apellido,pais,idioma,ipRegistro,cumpleaños,email,ultimaIP,pregunta,respuesta,apodo) 
			VALUES ('".$cuenta."','".$contraseña."','".$nombre."','".$apellido."','".$pais."','".$idioma."','".$ipregistro."','".$cumpleañosdia."~".$cumpleañosmes."~".$cumpleañosaño."','".$email."','".$ipregistro."','".$pregunta."','".$respuesta."','".$apodo."')");

			if ($sql = $serv->query($sentencia)){
				echo "&result=";
			}else{
				$mensaje = "&result=ERROR ".$serv->error;
				echo $mensaje;
			}
			
		}else{
			echo "&result=NO SE PUEDE REGISTRAR  ";
		}
	}
	else{
		echo "&result=ERROR CONEXION SQL ! ";
	}
}
?>