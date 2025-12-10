#include <stdio.h>

struct Student {
    char name[50];
    int age;
};

int main() {
    struct Student student;
    
    printf("학생 이름: ");
    scanf("%s", student.name);
    
    printf("학생 나이: ");
    scanf("%d", &student.age);
    
    printf("\n=== 학생 정보 ===\n");
    printf("이름: %s\n", student.name);
    printf("나이: %d\n", student.age);
    
    return 0;
}

