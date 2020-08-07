# FROM ubuntu:latest
FROM amazonlinux:latest

# RUN apt-get update -y && apt-get install -y zsh curl git steamcmd
RUN yum update -y && yum install -y zsh curl git
RUN amazon-linux-extras install java-openjdk11

RUN mkdir /local/game

RUN useradd -ms /bin/zsh server

USER server
WORKDIR /home/server

RUN sh -c "$(curl -fsSL https://raw.github.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"

WORKDIR /local/game

COPY --chown=server Builds/Release/Linux/Mono/Server .
RUN chmod +x Voxelfield
# RUN java -jar GameLiftLocal.jar -p 27015 &

# CMD ./Voxelfield
CMD zsh 

EXPOSE 27015/udp
EXPOSE 27015/tcp
