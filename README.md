# ShadowProject

깃허브 비공개 개인 리포지토리에서 공개하고 싶은 부분만 공개하고 싶어서 만든 프로그램



기본적으로 JSON문법과 PrelRegex문법을 알아야 합니다.



## Manifest

복제할 디렉터리와(SourceDir) 붙여넣을 디렉터리(DestDir)설정과 세부 옵션 파일



### 구조와 설명

* 배열 데이터는 추가가능
* 비주얼 스튜디오 솔루션 디렉터리에서 소스파일만 추출하는 예시
* 데이터 이름중 "A__" 접두사는 알파벳순서 대로 실행됨
* Selection.Enable이 비활성화 할경우 작업이 없음
* Selection.DirectorySelectionRegex or Selection.FileSelectionRegex의 하위 데이터중 Use 접두사를 가진 데이터가 비활성화 하면 정규식을 사용하지 않는다는 의미이고, 모두 허용(복사) 한다. 의미 입니다. 예시 :  A__Use<-(이것)RelativePathDirNameRegex

```
{
  //예외 무시 여부
  "IgnoreExceptions": false,
  //복제할 디렉터리
  "SourceDirectory": "C:\\SourceDir",
  //붙여 넣을 디렉터리. 자동으로 위치에 디렉터리가 생성됨
  "DestDirectory": "C:\\DestDir",
  //복제할 타겟 조건 옵션
  "Selection": {
    //디렉터리 선택 정규식
    "DirectorySelectionRegex": {
      //상대경로에서 정규식 사용
      //정확히 베이스경로가 없는 상대 경로 입니다
      //
      //예시
      //C:\\SourceDir
      //C:\\SourceDir\\ABC
      //베이스가 없는 상대경로
      //SourceDir
      //SourceDir\\ABC
      //해당 문자열로 정규식을 검사합니다
      "A__UseRelativePathDirNameRegex": true,
      //상대경로 정규식 bool 값 반전
      "A__RelativePathDirNameRegex__N__InvertValue": true,
      //상대 경로 정규식
      "A__RelativePathDirNameRegex": "(obj)|(Debug)|(Release)|(debug)|(release)|(bin)|(Bin)|(x64)|(x86)|(x86x64)|(Properties)",
      //디렉터리 이름만 정규식으로 검사함
      //"C:\\SourceDir\\ABC"에서 "ABC"만.
	  "B__UseDirNameRegex": true,
      "B__DirNameRegex__InvertValue": true,
      "B__DirNameRegex": "^((obj)|(Debug)|(Release)|(debug)|(release)|(bin)|(Bin)|(x64)|(x86)|(x86x64)|(Properties))$",
      //디렉터리 전체 경로를 정규식으로 검사함
      //"C:\\SourceDir\\ABC"에서 "C:\\SourceDir\\ABC"
      "C__UseDirPathRegex": false,
      "C__DirPathRegex__InvertValue": false,
      "C__DirPathRegex": "",
      //복사 조건 논리식
      //OR : 위의 정규식중 하나만 true가 나오면 복사 타겟
      //AND : 위의 정규식중 하나 이상 false가 나오면 제외
      "ChooseOneOf_AND_or_OR": "OR"
    },
    //파일 선택 정규식
    "FileSelectionRegex": {
      //"C:\\SourceDir\\ABC\\File.file"에서 "File"만
      "B__UseFileNameRegex": false,
      "B__FileNameRegex__InvertValue": false,
      "B__FileNameRegex": "",
      //"C:\\SourceDir\\ABC\\File.file"에서 "file"만
      "C__UseExtRegex": true,
      "C__ExtRegex__InvertValue": false,
      "C__ExtRegex": "^((cs)|(c)|(h)|(cpp))$",
      //"C:\\SourceDir\\ABC\\File.file"에서 "File.file"만
      "A__UseFileFullNameRegex": false,
      "A__FileFullNameRegex__InvertValue": false,
      "A__FileFullNameRegex": "",
      //"C:\\SourceDir\\ABC\\File.file"에서 "C:\\SourceDir\\ABC\\File.file"
      "D__UseFilePathRegex": false,
      "D__FilePathRegex__InvertValue": false,
      "D__FilePathRegex": "",
      "ChooseOneOf_AND_or_OR": "OR"
    },
    "Enable": true
  },
  //파일 교정
  "FileProofreader": {
    //텍스트 파일 교정. 문서 파일이 아님
    "TextFile__IsNotDocFile": [
      {
        //타겟 확정자
        "A__Extensions": [
          "cs",
          "cpp",
          "h",
          "c"
        ],
        //개행문자를 통일 하기위함
        //옵션
        //NEWLINE_CR : \r
        //NEWLINE_LF : \n
        //NEWLINE_CRLF : \r\n
        //NEWLINE_NONE : 수정하지 않음
        //NEWLINE_ERASE : 개행문자를 지움
        "B__PleaseSelectTheNewlineYouWant(NEWLINE_CR,NEWLINE_LF,NEWLINE_CRLF,NEWLINE_NONE,NEWLINE_ERASE)": "NEWLINE_LF",
        //C__접두사 데이터는 아직 사용할수없음
        "C__RemoveComment": true,
        "C__CommentRegex": {
          "SingleCommentSign": "//",
          "MultiCommentOpenSign": "/*",
          "MultiCommentCloseSign": "*/",
          "Enable": true
        },
        //들여쓰기 변환
        "D__IndentConverter": {
          //탭 하나당 공백 갯수
          "Space": 4,
          //위 공백 개수당 탭하나
          "Tab": 1,
          //공백에서 탭으로 변환 여부
          "ConvertSapceToTab": true,
          "Enable": true
        },
        //인코딩 변환
        "E__Encoding": {
          //해당 인코더로 모든 텍스트 파일을 인코딩함
          "EncodingName__IfEmptyDontConvertEncoding": "utf-8",
          "Enable": true
        },
        "Enable": true
      }
    ],
    "Enable": true
  },
  //파일 복사 버퍼
  "BufferSize": 4096
}
```

