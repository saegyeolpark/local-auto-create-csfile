# local-auto-create-csfile

## 1. Net 6.0 설치
프로젝트 빌드에 닷넷 6.0을 사용합니다.  
아래 링크로 이동하여 해당 sdk를 설치합니다.  
[**dotnet 6.0**](https://dotnet.microsoft.com/ko-kr/download/dotnet/6.0)   

</br>  

## 2. 유니티 프로젝트 오픈
***AutoGenerated*** 폴더를 Unity Hub를 이용해 한번 열어줍니다.  
자동으로 생성되는 스크립트가 <u>UnityEngine</u> 라이브러리를 참조하기 때문에, 그 오류를 없애기 위해 필요합니다.   

</br>

## 3. apiFile.json 수정
***apiFile.json***은 로컬생성에 참고할 프리셋 데이터입니다.  
새로운 데이터를 적용하고 싶다면, postman api에서 반환받은 json을 붙여넣기합니다.  
API: *{{BASE_URL}}/admin/sheet/list/convert-before-data?target=CSharp&version=1.0.0*  
  
</br>

## 4. bat 파일 수정
***launch.bat*** 파일을 우클릭하여 편집을 선택합니다.  
**set git_path=""**  
우측 따옴표 안에 [auto-create-csfile](https://github.com/gameduo/auto-create-csfile)을 clone 받아, 그 폴더의 절대 경로를 입력해 줍니다.  
   

</br>  

## 5. bat 파일 실행
***launch.bat*** 파일을 더블클릭하여 실행합니다.  
자동생성된 파일은 AutoGenerated/Assets/**LocalCreated** 폴더 안에 생성됩니다.  
AutoGenerated/**AutoGenerated.sln** 솔루션을 열어 자동 생성된 코드에서 오류가 발생하는지 확인합니다.  

</br>  
